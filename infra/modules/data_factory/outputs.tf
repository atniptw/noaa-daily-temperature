output "identity" {
  value = azurerm_data_factory.factory.identity[0].principal_id
}