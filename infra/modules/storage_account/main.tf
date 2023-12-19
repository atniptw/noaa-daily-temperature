resource "random_integer" "example" {
  min = 1
  max = 99
  keepers = {
    used_by = var.used_by
  }
}

resource "azurerm_storage_account" "example" {
  name                     = "devstnoaa03"
  resource_group_name      = var.resource_group_name
  location                 = var.location
  account_tier             = "Standard"
  account_replication_type = "LRS"

  tags = {
    used_by = random_integer.example.keepers.used_by
  }
}