namespace SupplyChainX.Application.Common.Configuration;

public class KafkaTopicOptions
{
    public const string SectionName = "Kafka:Topics";

    public string ProductEvents { get; set; } = "supplychainx.product.events";
    public string WarehouseEvents { get; set; } = "supplychainx.warehouse.events";
    public string InventoryEvents { get; set; } = "supplychainx.inventory.events";
}
