terraform {
  required_version = ">= 1.5.0"
  required_providers {
    google = {
      source  = "hashicorp/google"
      version = ">= 5.0.0"
    }
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

locals {
  cloudsql_connection_name = var.cloudsql_connection_name != "" ? var.cloudsql_connection_name : google_sql_database_instance.mysql.connection_name
}

resource "google_artifact_registry_repository" "backend" {
  location      = var.region
  repository_id = var.artifact_repo
  description   = "Backend container images"
  format        = "DOCKER"
}

resource "google_service_account" "run_sa" {
  account_id   = "template-backend-sa"
  display_name = "Cloud Run backend service account"
}

resource "google_project_iam_member" "run_sa_logging" {
  project = var.project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_project_iam_member" "run_sa_monitoring" {
  project = var.project_id
  role    = "roles/monitoring.metricWriter"
  member  = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_project_iam_member" "run_sa_cloudsql" {
  project = var.project_id
  role    = "roles/cloudsql.client"
  member  = "serviceAccount:${google_service_account.run_sa.email}"
}

resource "google_sql_database_instance" "mysql" {
  name             = var.sql_instance_name
  database_version = "MYSQL_8_0"
  region           = var.region

  settings {
    tier = var.sql_tier
    ip_configuration {
      ipv4_enabled    = true
      private_network = var.vpc_network_self_link
    }
  }
}

resource "google_sql_database" "app_db" {
  name     = var.sql_database_name
  instance = google_sql_database_instance.mysql.name
}

resource "google_sql_user" "app_user" {
  name     = var.sql_user
  instance = google_sql_database_instance.mysql.name
  password = var.sql_password
}

resource "google_cloud_run_service" "backend" {
  count    = var.enable_cloud_run ? 1 : 0
  name     = var.cloud_run_service_name
  location = var.region

  template {
    metadata {
      annotations = {
        "run.googleapis.com/cloudsql-instances" = local.cloudsql_connection_name
      }
    }
    spec {
      service_account_name = google_service_account.run_sa.email
      containers {
        image = var.container_image

        env {
          name  = "DB_CONNECTION_STRING"
          value = var.db_connection_string
        }

        resources {
          limits = {
            cpu    = "1"
            memory = "512Mi"
          }
        }
      }
    }
  }

  traffic {
    percent         = 100
    latest_revision = true
  }
}

resource "google_cloud_run_service_iam_member" "public_invoker" {
  count    = var.enable_cloud_run ? 1 : 0
  location = google_cloud_run_service.backend[0].location
  service  = google_cloud_run_service.backend[0].name
  role     = "roles/run.invoker"
  member   = "allUsers"
}
