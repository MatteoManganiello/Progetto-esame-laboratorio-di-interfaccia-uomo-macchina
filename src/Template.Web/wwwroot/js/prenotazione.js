const { createApp } = Vue;
        createApp({
            data() {

                const dbData = window.dashboardData || {};

                return {
                    loading: false,
                    loadingBtn: false,
                    dataSelezionata: new Date().toISOString().split('T')[0],
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
                    menuSettimanale: dbData.menuSettimanale || {
                        'Lun': 'Lasagne alla Bolognese',
                        'Mar': 'Risotto ai Funghi',
                        'Mer': 'Pollo al Curry',
                        'Gio': 'Gnocchi al Pesto',
                        'Ven': 'Orata con Patate'
                    }
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
                    this.loading = true;
                    this.postazioneSelezionata = null;
                    fetch(`/Prenotazione/GetDatiMappa?data=${this.dataSelezionata}`)
                        .then(r => r.json())
                        .then(d => {
                            this.postazioni = d;
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