const { createApp } = Vue;

function normalizeMenuSettimanale(rawMenu) {
    if (!rawMenu) return null;

    let menu = rawMenu;
    if (typeof menu === 'string') {
        try {
            menu = JSON.parse(menu);
        } catch {
            return null;
        }
    }

    if (typeof menu !== 'object') return null;

    const fullToShort = {
        lunedi: 'Lun',
        martedi: 'Mar',
        mercoledi: 'Mer',
        giovedi: 'Gio',
        venerdi: 'Ven',
        sabato: 'Sab',
        domenica: 'Dom'
    };

    const normalized = {};
    Object.keys(menu).forEach((key) => {
        const lowerKey = key.toLowerCase();
        const shortKey = fullToShort[lowerKey] || key;
        if (menu[key]) {
            normalized[shortKey] = menu[key];
        }
    });

    return Object.keys(normalized).length > 0 ? normalized : null;
}

        createApp({
            data() {

                const dbData = window.dashboardData || {};
                const normalizedMenu = normalizeMenuSettimanale(dbData.menuSettimanale);
                const today = new Date();
                const minDateObj = new Date(today);
                const minDate = minDateObj.toISOString().split('T')[0];

                return {
                    loading: false,
                    loadingBtn: false,
                    minDate,
                    dataSelezionata: minDate,
                    postazioni: [],
                    postazioneSelezionata: null,
                    numeroPostiRichiesti: 1,
                    tooltip: { visibile: false, dati: null, x: 0, y: 0 },

                    carrello: [],


                    nomePrenotazione: dbData.nomeUtente || '',

                    storicoOrdini: dbData.storicoOrdini || [],
                    novita: dbData.novita || [
                        { data: '17/12/2025', titolo: 'Manutenzione Wi-Fi', contenuto: 'Manutenzione Wi-Fi dalle 17 alle 18' },
                        { data: '22/11/2025', titolo: 'Nuova Area meeting', contenuto: 'Apertura della seconda sala meeting' }
                    ],
                    menuSettimanale: normalizedMenu || {},
                    menuWarning: dbData.menuWarning ?? (normalizedMenu == null),
                    menuWeekStart: dbData.weekStart || null
                }
            },

            computed: {
                stats() {
                    if (!this.postazioni || this.postazioni.length === 0) {
                        return { totali: 0, occupati: 0, liberi: 0, percentuale: 0 };
                    }

                    let totali = 0;
                    let occupati = 0;

                    this.postazioni.forEach(p => {
                        if (p.tipo === 'Ristorante') {
                            totali += p.postiTotali;
                            occupati += p.postiOccupati;
                        } else {
                            totali += 1;
                            occupati += (p.isOccupata ? 1 : 0);
                        }
                    });

                    let liberi = totali - occupati;
                    let perc = totali > 0 ? Math.round((occupati / totali) * 100) : 0;

                    return { totali, occupati, liberi, percentuale: perc };
                }
            },

            mounted() {
                this.caricaDatiMappa();
            },

            methods: {
                caricaDatiMappa() {
                    if (this.dataSelezionata < this.minDate) {
                        this.dataSelezionata = this.minDate;
                    }
                    this.loading = true;
                    this.postazioneSelezionata = null;
                    fetch(`/Prenotazione/GetDatiMappa?data=${this.dataSelezionata}`)
                        .then(r => r.json())
                        .then(d => {
                            if (Array.isArray(d)) {
                                this.postazioni = d;
                            } else {
                                this.postazioni = d.postazioni || [];
                                const normalizedMenu = normalizeMenuSettimanale(d.menuSettimanale);
                                this.menuSettimanale = normalizedMenu || {};
                                this.menuWarning = d.menuWarning ?? (normalizedMenu == null);
                                this.menuWeekStart = d.weekStart || null;
                            }
                            this.loading = false;
                        })
                        .catch(e => { console.error(e); this.loading = false; });
                },

                getStatoClasse(p) {
                    if (this.postazioneSelezionata && this.postazioneSelezionata.id === p.id) return "status-selected";

                    if (this.carrello.some(x => x.postazione.id === p.id)) return "status-selected";

                    if (p.tipo === 'Ristorante') {
                        if (p.postiOccupati >= p.postiTotali) return "status-booked";
                        if (p.postiOccupati > 0) return "status-partial";
                        return "status-free";
                    }
                    return p.isOccupata ? "status-booked" : "status-free";
                },

                selezionaPostazione(p) {

                    if (p.tipo === 'Ristorante' && p.postiOccupati >= p.postiTotali) return;
                    if (p.tipo !== 'Ristorante' && p.isOccupata) return;

                    this.postazioneSelezionata = p;
                    this.numeroPostiRichiesti = 1;

                    if (!this.nomePrenotazione && window.dashboardData && window.dashboardData.nomeUtente) {
                        this.nomePrenotazione = window.dashboardData.nomeUtente;
                    }
                },


                aggiungiAlCarrello() {
                    if (!this.postazioneSelezionata) return;

                    let index = this.carrello.findIndex(x => x.postazione.id === this.postazioneSelezionata.id);

                    let item = {
                        postazione: this.postazioneSelezionata,
                        numeroPersone: parseInt(this.numeroPostiRichiesti)
                    };

                    if (index > -1) {

                        this.carrello[index] = item;
                    } else {

                        this.carrello.push(item);
                    }


                    this.postazioneSelezionata = null;
                },

                rimuoviDalCarrello(id) {
                    this.carrello = this.carrello.filter(x => x.postazione.id !== id);
                },

                confermaPrenotazione() {
 
                    if (this.postazioneSelezionata) {
                        this.aggiungiAlCarrello();
                    }

                    if (this.carrello.length === 0) return;

                    this.loadingBtn = true;

                    let elementiApi = this.carrello.map(item => ({
                        PostazioneId: item.postazione.id,
                        NumeroPersone: item.numeroPersone
                    }));

                    let payload = {
                        Data: this.dataSelezionata,
                        Elementi: elementiApi, 

                    };

                    fetch('/Prenotazione/Prenota', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(payload)
                    }).then(async r => {
                        this.loadingBtn = false;
                        const res = await r.json();

                        if (r.ok) {
                            alert(" " + res.message);

                            this.carrello.forEach(item => {
                                this.storicoOrdini.unshift({
                                    data: 'Adesso',
                                    descrizione: item.postazione.nome + (item.postazione.tipo === 'Ristorante' ? ` (x${item.numeroPersone})` : ''),
                                    tipo: item.postazione.tipo,
                                    prezzo: this.getPrezzoItem(item)
                                });
                            });

                       
                            this.carrello = [];
                            this.postazioneSelezionata = null;
                            this.caricaDatiMappa(); 
                        } else {
                            alert(" Errore: " + (res.message || "Qualcosa è andato storto"));
                        }
                    }).catch(err => {
                        this.loadingBtn = false;
                        console.error(err);
                        alert(" Errore di connessione al server.");
                    });
                },

                getPrezzoUnitario(tipo) {
                    switch (tipo) {
                        case 'Singola': return 25;
                        case 'Team': return 150;
                        case 'Riunioni': return 80;
                        case 'Eventi': return 500;
                        case 'Ristorante': return 15;
                        default: return 0;
                    }
                },

                getPrezzoItem(item) {
                    let base = this.getPrezzoUnitario(item.postazione.tipo);
                    return item.postazione.tipo === 'Ristorante' ? base * item.numeroPersone : base;
                },

                getTotaleCarrello() {
                    return this.carrello.reduce((acc, item) => acc + this.getPrezzoItem(item), 0);
                },

                getPrezzoTotale() {
                    if (!this.postazioneSelezionata) return 0;
                    let base = this.getPrezzoUnitario(this.postazioneSelezionata.tipo);
                    return this.postazioneSelezionata.tipo === 'Ristorante' ? base * this.numeroPostiRichiesti : base;
                },

                mostraTooltip(postazione, event) {
                    this.tooltip.dati = postazione;
                    this.tooltip.visibile = true;
                    this.muoviTooltip(event);
                },
                muoviTooltip(event) {
                    this.tooltip.x = event.clientX;
                    this.tooltip.y = event.clientY;
                },
                nascondiTooltip() {
                    this.tooltip.visibile = false;
                }
            }
        }).mount('#app-prenotazione');