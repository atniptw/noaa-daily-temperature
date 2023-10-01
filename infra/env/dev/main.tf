locals {
  resource_group = "dev-rg-noaa"
}

module "infra" {
  source = "../../"

  resource_group = local.resource_group
  environment    = "dev"
}