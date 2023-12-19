output "hostname" {
  value = azurerm_linux_function_app.example.default_hostname
}

output "function_key" {
  value = data.azurerm_function_app_host_keys.example.default_function_key
}
