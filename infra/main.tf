provider "aws" {
  region = var.aws_region

  default_tags {
    tags = {
      Project     = "blockchain-tracker"
      ManagedBy   = "terraform"
      Environment = var.environment
    }
  }
}

data "aws_availability_zones" "available" {
  state = "available"
}

data "aws_caller_identity" "current" {}
