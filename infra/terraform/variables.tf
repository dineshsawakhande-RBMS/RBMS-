variable "project_name" {
  description = "Short project identifier used as a prefix for resource names."
  type        = string
  default     = "rbms"
}

variable "environment" {
  description = "Deployment environment (e.g. dev, staging, production)."
  type        = string
  default     = "production"
}

variable "aws_region" {
  description = "Primary AWS region for the deployment."
  type        = string
  default     = "ap-south-1"
}

# --- Networking ---
variable "vpc_cidr" {
  description = "CIDR block for the VPC."
  type        = string
  default     = "10.0.0.0/16"
}

variable "availability_zones" {
  description = "AZs to spread subnets across (must be at least 2)."
  type        = list(string)
  default     = ["ap-south-1a", "ap-south-1b"]
}

variable "public_subnet_cidrs" {
  description = "CIDR blocks for public subnets (one per AZ)."
  type        = list(string)
  default     = ["10.0.0.0/24", "10.0.1.0/24"]
}

variable "private_subnet_cidrs" {
  description = "CIDR blocks for private subnets (one per AZ)."
  type        = list(string)
  default     = ["10.0.10.0/24", "10.0.11.0/24"]
}

# --- RDS ---
variable "db_name" {
  description = "PostgreSQL database name."
  type        = string
  default     = "rbms"
}

variable "db_username" {
  description = "Master username for the database."
  type        = string
  default     = "rbms_admin"
}

variable "db_instance_class" {
  description = "RDS instance class."
  type        = string
  default     = "db.t3.medium"
}

variable "db_allocated_storage" {
  description = "Allocated storage in GB."
  type        = number
  default     = 50
}

variable "db_engine_version" {
  description = "PostgreSQL engine version."
  type        = string
  default     = "16.4"
}

variable "db_multi_az" {
  description = "Enable Multi-AZ for the RDS instance."
  type        = bool
  default     = true
}

variable "db_backup_retention_days" {
  description = "Automated backup retention window in days."
  type        = number
  default     = 14
}

# --- ECS / API ---
variable "api_image" {
  description = "Full ECR image URI (with tag) for the API container."
  type        = string
  default     = ""
}

variable "api_container_port" {
  description = "Port the API container listens on."
  type        = number
  default     = 8080
}

variable "api_cpu" {
  description = "Fargate task CPU units."
  type        = number
  default     = 512
}

variable "api_memory" {
  description = "Fargate task memory (MiB)."
  type        = number
  default     = 1024
}

variable "api_desired_count" {
  description = "Baseline number of API tasks."
  type        = number
  default     = 2
}

variable "api_min_count" {
  description = "Minimum tasks for autoscaling."
  type        = number
  default     = 2
}

variable "api_max_count" {
  description = "Maximum tasks for autoscaling."
  type        = number
  default     = 6
}

variable "acm_certificate_arn" {
  description = "ACM certificate ARN (in aws_region) for the ALB HTTPS listener. Leave empty to serve HTTP only."
  type        = string
  default     = ""
}

# --- Secrets ---
variable "jwt_issuer" {
  description = "Issuer claim placed in the JWT secret payload."
  type        = string
  default     = "rbms-api"
}
