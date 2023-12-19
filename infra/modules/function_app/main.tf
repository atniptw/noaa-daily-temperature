locals {
  function_app_name = "devfuncnoaa01"
}

module "storage_account" {
  source = "../storage_account"

  resource_group_name = var.resource_group_name
  location            = var.location
  used_by             = local.function_app_name
}

resource "azurerm_service_plan" "example" {
  name                = "devaspnoaa"
  resource_group_name = var.resource_group_name
  location            = var.location
  os_type             = "Linux"
  sku_name            = "Y1"
}

resource "azurerm_linux_function_app" "example" {
  name                = local.function_app_name
  resource_group_name = var.resource_group_name
  location            = var.location

  storage_account_name       = module.storage_account.name
  storage_account_access_key = module.storage_account.primary_access_key
  service_plan_id            = azurerm_service_plan.example.id
  client_certificate_mode    = "Required"

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      dotnet_version              = "8.0"
      use_dotnet_isolated_runtime = true
    }
  }
}

data "azurerm_function_app_host_keys" "example" {
  name                = azurerm_linux_function_app.example.name
  resource_group_name = var.resource_group_name
}
