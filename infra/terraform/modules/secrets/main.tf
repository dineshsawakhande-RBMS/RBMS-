terraform {
  required_providers {
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

# --- Database master password ---
resource "random_password" "db" {
  length  = 32
  special = true
  # RDS disallows a handful of characters in the master password.
  override_special = "!#$%^&*()-_=+[]{}<>?"
}

resource "aws_secretsmanager_secret" "db" {
  name                    = "${var.name_prefix}/db-credentials"
  description             = "PostgreSQL master credentials for ${var.name_prefix}"
  recovery_window_in_days = 7
}

resource "aws_secretsmanager_secret_version" "db" {
  secret_id = aws_secretsmanager_secret.db.id
  secret_string = jsonencode({
    username = var.db_username
    password = random_password.db.result
    dbname   = var.db_name
    port     = var.db_port
    engine   = "postgres"
  })
}

# --- JWT signing key ---
resource "random_password" "jwt" {
  length  = 64
  special = false
}

resource "aws_secretsmanager_secret" "jwt" {
  name                    = "${var.name_prefix}/jwt-signing-key"
  description             = "JWT HMAC signing key for ${var.name_prefix} API"
  recovery_window_in_days = 7
}

resource "aws_secretsmanager_secret_version" "jwt" {
  secret_id = aws_secretsmanager_secret.jwt.id
  secret_string = jsonencode({
    issuer    = var.jwt_issuer
    audience  = var.jwt_issuer
    signinKey = random_password.jwt.result
  })
}
