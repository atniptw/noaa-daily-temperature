locals {
  data_factory_name = "devadfnoaa"
}

resource "azurerm_data_factory" "factory" {
  name                = local.data_factory_name
  location            = var.location
  resource_group_name = var.resource_group_name

  identity {
    type = "SystemAssigned"
  }
}

##################################################################
#                        Storage Account                         #
##################################################################

module "storage_account" {
  source = "../storage_account"

  location            = var.location
  resource_group_name = var.resource_group_name
  used_by             = local.data_factory_name
}

resource "azurerm_role_assignment" "example" {
  scope                = module.storage_account.id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_data_factory.factory.identity[0].principal_id
}

resource "azurerm_storage_container" "ghcn" {
  name                  = "ghcn-by-year"
  storage_account_name  = module.storage_account.name
  container_access_type = "private"
}

resource "azurerm_storage_container" "stations" {
  name                  = "ghcnd-stations"
  storage_account_name  = module.storage_account.name
  container_access_type = "private"
}

resource "azurerm_storage_management_policy" "example" {
  storage_account_id = module.storage_account.id

  rule {
    name    = "rule1"
    enabled = true
    filters {
      blob_types = ["blockBlob"]
      prefix_match = [ "ghcn-by-year/" ]
    }
    actions {
      base_blob {
        delete_after_days_since_modification_greater_than = 1
      }
      snapshot {
        delete_after_days_since_creation_greater_than = 1
      }
    }
  }
}


##################################################################
#                        Linked Services                         #
##################################################################

resource "azurerm_data_factory_linked_service_azure_blob_storage" "ghcn" {
  name                       = "ghcn_blob_storage"
  data_factory_id            = azurerm_data_factory.factory.id
  connection_string_insecure = module.storage_account.primary_blob_connection_string
  storage_kind               = "BlobStorage"
  use_managed_identity       = true
}

resource "azurerm_data_factory_linked_service_cosmosdb" "ghcn" {
  name              = "ghcn_cosmosdb"
  data_factory_id   = azurerm_data_factory.factory.id
  connection_string = var.cosmos_connection_string
  database          = var.cosmosdb_name
}

resource "azurerm_data_factory_linked_custom_service" "ghcn" {
  name                 = "ghcn_http"
  data_factory_id      = azurerm_data_factory.factory.id
  type                 = "HttpServer"
  type_properties_json = <<JSON
{
    "url": "https://www.ncei.noaa.gov/",
    "enableServerCertificateValidation": true,
    "authenticationType": "Anonymous"
}
JSON
}

resource "azurerm_data_factory_linked_service_azure_function" "ghcn" {
  name            = "ghcn_function"
  data_factory_id = azurerm_data_factory.factory.id
  url             = "https://${var.function_app_hostname}"
  key             = var.function_app_key
}

##################################################################
#                         Datasets                               #
##################################################################

resource "azurerm_data_factory_dataset_http" "ghcn_by_year_source" {
  name                = "ghcn_by_year_source"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_custom_service.ghcn.name

  relative_url   = "@concat('/pub/data/ghcn/daily/by_year/', dataset().year, '.csv.gz')"
  request_method = "GET"

  parameters = {
    "year" = ""
  }
}

resource "azurerm_data_factory_dataset_binary" "ghcn_by_year_binary_sink" {
  name                = "ghcn_by_year_binary_sink"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name

  azure_blob_storage_location {
    container = azurerm_storage_container.ghcn.name
    path      = "compressed"
  }
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_by_year_binary_source" {
  name                = "ghcn_by_year_binary_source"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  compression_codec   = "gzip"
  column_delimiter    = ","
  row_delimiter       = "\n"

  parameters = {
    filename = ""
  }

  azure_blob_storage_location {
    container                = azurerm_storage_container.ghcn.name
    path                     = "compressed"
    dynamic_filename_enabled = true
    filename                 = "@dataset().filename"
  }

}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_by_year_delimited_text_sink" {
  name                = "ghcn_by_year_delimited_text_sink"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","
  row_delimiter       = "\n"

  azure_blob_storage_location {
    container = azurerm_storage_container.ghcn.name
    path      = "extracted"
  }
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcn_by_year_delimited_text_source" {
  name                = "ghcn_by_year_delimited_text_source"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","

  parameters = {
    filename = ""
  }

  azure_blob_storage_location {
    container                = azurerm_storage_container.ghcn.name
    dynamic_filename_enabled = true
    path                     = "extracted"
    filename                 = "@dataset().filename"
  }

}

resource "azurerm_data_factory_dataset_cosmosdb_sqlapi" "ghcn_by_year_cosmosdb_sink" {
  name                = "ghcn_by_year_cosmosdb_sink"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_cosmosdb.ghcn.name

  collection_name = "ghcn"
}

resource "azurerm_data_factory_dataset_http" "ghcnd_stations_source" {
  name                = "ghcnd_stations_source"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_custom_service.ghcn.name

  relative_url   = "pub/data/ghcn/daily/ghcnd-stations.txt"
  request_method = "GET"
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcnd_stations_delimited_text_sink" {
  name                = "ghcnd_stations_delimited_text_sink"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","
  row_delimiter       = "\n"

  azure_blob_storage_location {
    container = azurerm_storage_container.stations.name
  }
}

resource "azurerm_data_factory_dataset_binary" "ghcnd_stations_sink" {
  name                = "ghcnd_stations_sink"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name

  azure_blob_storage_location {
    container = azurerm_storage_container.stations.name
  }
}

resource "azurerm_data_factory_dataset_delimited_text" "ghcnd_stations_delimited_text_source" {
  name                = "ghcnd_stations_delimited_text_source"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_azure_blob_storage.ghcn.name
  column_delimiter    = ","

  parameters = {
    filename = ""
  }

  azure_blob_storage_location {
    container                = azurerm_storage_container.stations.name
    dynamic_filename_enabled = true
    filename                 = "@dataset().filename"
  }

}

resource "azurerm_data_factory_dataset_cosmosdb_sqlapi" "ghcnd_stations_cosmosdb_sink" {
  name                = "ghcnd_stations_cosmosdb_sink"
  data_factory_id     = azurerm_data_factory.factory.id
  linked_service_name = azurerm_data_factory_linked_service_cosmosdb.ghcn.name

  collection_name = "stations"
}

##################################################################
#                         Data Flow                              #
##################################################################

resource "azurerm_data_factory_data_flow" "ghcn_by_year" {
  name            = "ghcn_by_year"
  data_factory_id = azurerm_data_factory.factory.id

  source {
    name = "ReadingSource"

    dataset {
      name = azurerm_data_factory_dataset_delimited_text.ghcn_by_year_delimited_text_source.name
    }
  }

  source {
    name = "StationSource"

    dataset {
      name = "Json1"
    }
  }

  transformation {
    name = "RemoveNullValues"
  }
  transformation {
    name = "RenameColumns"
  }
  transformation {
    name = "AggregateRecords"
  }
  transformation {
    name = "ConvertToDecimalValue"
  }
  transformation {
    name = "AddCombinedField"
  }
  transformation {
    name = "JoinReadingsWithStations"
  }
  transformation {
    name = "AddKnownId"
  }
  transformation {
    name = "FinalFieldMapping"
  }

  sink {
    name = "CosmosDB"

    dataset {
      name = azurerm_data_factory_dataset_cosmosdb_sqlapi.ghcn_by_year_cosmosdb_sink.name
    }
  }

script_lines = [
                "source(output(",
                "          Column_1 as string,",
                "          Column_2 as string,",
                "          Column_3 as string,",
                "          Column_4 as string,",
                "          Column_5 as string,",
                "          Column_6 as string,",
                "          Column_7 as string,",
                "          Column_8 as string",
                "     ),",
                "     allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     ignoreNoFilesFound: false) ~> ReadingSource",
                "source(output(",
                "          id as string,",
                "          name as string,",
                "          location as (type as string, coordinates as double[]),",
                "          elevation as double,",
                "          state as string,",
                "          flags as string[],",
                "          wmoId as string",
                "     ),",
                "     allowSchemaDrift: false,",
                "     validateSchema: false,",
                "     ignoreNoFilesFound: false,",
                "     documentForm: 'documentPerLine') ~> StationSource",
                "ReadingSource filter(not(isNull(Column_1)) && not(isNull(Column_2)) && not(isNull(Column_3)) && not(isNull(Column_4))) ~> RemoveNullValues",
                "RemoveNullValues select(mapColumn(",
                "          stationId = Column_1,",
                "          date = Column_2,",
                "          recordType = Column_3,",
                "          value = Column_4,",
                "          measurementFlag = Column_5,",
                "          qualityFlag = Column_6,",
                "          sourceFlag = Column_7,",
                "          observationTime = Column_8",
                "     ),",
                "     skipDuplicateMapInputs: true,",
                "     skipDuplicateMapOutputs: true) ~> RenameColumns",
                "AddCombinedField aggregate(groupBy(stationId,",
                "          date),",
                "     readings = collect(temp)) ~> AggregateRecords",
                "RenameColumns derive(value = divide(toInteger(value), 10)) ~> ConvertToDecimalValue",
                "ConvertToDecimalValue derive(temp = @(recordType,value,measurementFlag,qualityFlag,sourceFlag,observationTime)) ~> AddCombinedField",
                "AggregateRecords, StationSource join(stationId == id,",
                "     joinType:'inner',",
                "     matchType:'exact',",
                "     ignoreSpaces: false,",
                "     broadcast: 'auto')~> JoinReadingsWithStations",
                "JoinReadingsWithStations derive(newId = concat(stationId,\"+\",date)) ~> AddKnownId",
                "AddKnownId select(mapColumn(",
                "          id = newId,",
                "          stationId,",
                "          date,",
                "          readings,",
                "          name,",
                "          location,",
                "          elevation,",
                "          state,",
                "          flags,",
                "          wmoId",
                "     ),",
                "     skipDuplicateMapInputs: true,",
                "     skipDuplicateMapOutputs: true) ~> FinalFieldMapping",
                "FinalFieldMapping sink(allowSchemaDrift: true,",
                "     validateSchema: false,",
                "     deletable:false,",
                "     insertable:true,",
                "     updateable:false,",
                "     upsertable:false,",
                "     format: 'document',",
                "     throughput: 400,",
                "     skipDuplicateMapInputs: true,",
                "     skipDuplicateMapOutputs: true) ~> CosmosDB"
            ]
}

resource "azurerm_data_factory_data_flow" "ghcnd_stations" {
  name            = "ghcnd_stations"
  data_factory_id = azurerm_data_factory.factory.id

  source {
    name = "source1"

    dataset {
      name = azurerm_data_factory_dataset_delimited_text.ghcnd_stations_delimited_text_source.name
    }
  }

  sink {
    name = "sink1"

    dataset {
      name = azurerm_data_factory_dataset_cosmosdb_sqlapi.ghcnd_stations_cosmosdb_sink.name
    }
  }

  transformation {
    name = "select1"
  }

  script_lines = [
    "source(output(",
    "          Column_1 as string",
    "     ),",
    "     allowSchemaDrift: true,",
    "     validateSchema: false,",
    "     ignoreNoFilesFound: false) ~> source1",
    "source1 select(mapColumn(",
    "          record = Column_1",
    "     ),",
    "     skipDuplicateMapInputs: true,",
    "     skipDuplicateMapOutputs: true) ~> select1",
    "select1 sink(allowSchemaDrift: true,",
    "     validateSchema: false,",
    "     deletable:false,",
    "     insertable:true,",
    "     updateable:false,",
    "     upsertable:false,",
    "     format: 'document',",
    "     skipDuplicateMapInputs: true,",
    "     skipDuplicateMapOutputs: true) ~> sink1"
  ]
}
