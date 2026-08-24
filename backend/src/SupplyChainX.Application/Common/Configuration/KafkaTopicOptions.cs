namespace SupplyChainX.Application.Common.Configuration;

public class KafkaTopicOptions
{
    public const string SectionName = "Kafka:Topics";

    public string ProductEvents { get; set; } = "supplychainx.product.events";
    public string WarehouseEvents { get; set; } = "supplychainx.warehouse.events";
    public string InventoryEvents { get; set; } = "supplychainx.inventory.events";

    public string ProductEventsDlq { get; set; } = "supplychainx.product.events.dlq";
    public string WarehouseEventsDlq { get; set; } = "supplychainx.warehouse.events.dlq";
    public string InventoryEventsDlq { get; set; } = "supplychainx.inventory.events.dlq";

    /// <summary>
    /// Maps a primary event topic to its corresponding Dead Letter Queue (DLQ) topic.
    /// </summary>
    public string GetDlqTopic(string originalTopic)
    {
        if (originalTopic == ProductEvents) return ProductEventsDlq;
        if (originalTopic == WarehouseEvents) return WarehouseEventsDlq;
        if (originalTopic == InventoryEvents) return InventoryEventsDlq;

        return $"{originalTopic}.dlq";
    }
}
