terraform {
  required_version = ">= 1.6.0"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  # Remote state backend. Create the bucket + DynamoDB lock table once
  # (see infra/terraform/README.md) and then uncomment + run `terraform init`.
  #
  # backend "s3" {
  #   bucket         = "rbms-terraform-state-<account-id>"
  #   key            = "rbms/terraform.tfstate"
  #   region         = "ap-south-1"
  #   dynamodb_table = "rbms-terraform-locks"
  #   encrypt        = true
  # }
}
