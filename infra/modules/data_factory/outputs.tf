output "identity" {
  value = azurerm_DataFactoryTest.factory.identity[0].principal_id
}

output "id" {
  value = azurerm_DataFactoryTest.factory.id
}