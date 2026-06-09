locals {
  name_prefix = "${var.project_name}-${var.environment}"
}

module "networking" {
  source = "./modules/networking"

  name_prefix          = local.name_prefix
  vpc_cidr             = var.vpc_cidr
  availability_zones   = var.availability_zones
  public_subnet_cidrs  = var.public_subnet_cidrs
  private_subnet_cidrs = var.private_subnet_cidrs
  api_container_port   = var.api_container_port
  db_port              = 5432
}

module "secrets" {
  source = "./modules/secrets"

  name_prefix = local.name_prefix
  db_name     = var.db_name
  db_username = var.db_username
  db_port     = 5432
  jwt_issuer  = var.jwt_issuer
}

module "rds" {
  source = "./modules/rds"

  name_prefix           = local.name_prefix
  private_subnet_ids    = module.networking.private_subnet_ids
  rds_security_group_id = module.networking.rds_security_group_id

  db_name               = var.db_name
  db_username           = var.db_username
  db_password           = module.secrets.db_password
  instance_class        = var.db_instance_class
  allocated_storage     = var.db_allocated_storage
  engine_version        = var.db_engine_version
  multi_az              = var.db_multi_az
  backup_retention_days = var.db_backup_retention_days
}

module "ecs" {
  source = "./modules/ecs"

  name_prefix = local.name_prefix
  aws_region  = var.aws_region

  vpc_id                = module.networking.vpc_id
  public_subnet_ids     = module.networking.public_subnet_ids
  private_subnet_ids    = module.networking.private_subnet_ids
  alb_security_group_id = module.networking.alb_security_group_id
  ecs_security_group_id = module.networking.ecs_security_group_id

  api_image           = var.api_image
  container_port      = var.api_container_port
  task_cpu            = var.api_cpu
  task_memory         = var.api_memory
  desired_count       = var.api_desired_count
  min_count           = var.api_min_count
  max_count           = var.api_max_count
  acm_certificate_arn = var.acm_certificate_arn

  # Secrets injected into the task at runtime.
  db_secret_arn  = module.secrets.db_secret_arn
  jwt_secret_arn = module.secrets.jwt_secret_arn

  # Non-secret DB connection details (host comes from RDS).
  db_host     = module.rds.db_address
  db_port     = 5432
  db_name     = var.db_name
  db_username = var.db_username
}

module "s3_cloudfront" {
  source = "./modules/s3_cloudfront"

  providers = {
    aws           = aws
    aws.us_east_1 = aws.us_east_1
  }

  name_prefix = local.name_prefix
}
