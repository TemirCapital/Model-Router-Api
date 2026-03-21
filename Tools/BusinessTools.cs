namespace ModelRouterApi.Tools;

/// <summary>
/// Contract for every pluggable business tool.
/// Each tool is responsible for parsing the user prompt, calling the downstream
/// API/service, and returning raw structured data (JSON string).
/// The ModelService will then summarise this for the user.
/// </summary>
public interface IBusinessTool
{
    string ToolName { get; }
    Task<string> ExecuteAsync(string prompt);
}

// ─────────────────────────────────────────────────────────────────────────────
// CRM Tool — simulates a Dynamics 365 / Salesforce customer lookup
// ─────────────────────────────────────────────────────────────────────────────
public class CrmTool : IBusinessTool
{
    public string ToolName => "CrmTool";

    public Task<string> ExecuteAsync(string prompt)
    {
        // In production: extract entity ID from prompt using NER or regex,
        // then call your CRM REST API with HttpClient (injected via DI).
        var mockData = """
        {
          "customerId": "CUST-4821",
          "name": "Contoso Ltd.",
          "status": "Active",
          "tier": "Gold",
          "accountManager": "Priya Sharma",
          "openCases": 2,
          "lastPurchaseDate": "2025-11-14",
          "annualRevenue": "$1.2M"
        }
        """;
        return Task.FromResult(mockData);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Order Management Tool — simulates an ERP / Order API call
// ─────────────────────────────────────────────────────────────────────────────
public class OrderManagementTool : IBusinessTool
{
    public string ToolName => "OrderManagementTool";

    public Task<string> ExecuteAsync(string prompt)
    {
        var mockData = """
        {
          "orderId": "ORD-98231",
          "customer": "Contoso Ltd.",
          "status": "Shipped",
          "shippedDate": "2026-03-18",
          "expectedDelivery": "2026-03-22",
          "items": [
            { "sku": "PRD-001", "name": "Azure Arc Appliance", "qty": 2, "unitPrice": 4500 },
            { "sku": "PRD-017", "name": "Edge Gateway Module", "qty": 5, "unitPrice": 850 }
          ],
          "totalValue": "$13,250",
          "carrier": "FedEx",
          "trackingNumber": "7489239847234"
        }
        """;
        return Task.FromResult(mockData);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Case / Helpdesk Tool — simulates ServiceNow / Dynamics Cases
// ─────────────────────────────────────────────────────────────────────────────
public class CaseTool : IBusinessTool
{
    public string ToolName => "CaseTool";

    public Task<string> ExecuteAsync(string prompt)
    {
        var mockData = """
        {
          "caseId": "CASE-7712",
          "subject": "Azure integration latency issue",
          "priority": "High",
          "status": "In Progress",
          "assignedTo": "Support Tier 2",
          "createdDate": "2026-03-19",
          "lastUpdated": "2026-03-21",
          "resolutionEta": "2026-03-23"
        }
        """;
        return Task.FromResult(mockData);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Scheduler Tool — simulates meeting/appointment booking
// ─────────────────────────────────────────────────────────────────────────────
public class SchedulerTool : IBusinessTool
{
    public string ToolName => "SchedulerTool";

    public Task<string> ExecuteAsync(string prompt)
    {
        var mockData = """
        {
          "action": "booked",
          "meetingId": "MTG-3341",
          "title": "Quarterly Business Review",
          "date": "2026-04-01",
          "time": "10:00 AM IST",
          "attendees": ["priya.sharma@contoso.com", "john.doe@fabrikam.com"],
          "conferenceLink": "https://teams.microsoft.com/l/meeting/xyz"
        }
        """;
        return Task.FromResult(mockData);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Generic Action Tool — catch-all for create/update/delete without a specific tool
// ─────────────────────────────────────────────────────────────────────────────
public class GenericActionTool : IBusinessTool
{
    public string ToolName => "GenericActionTool";

    public Task<string> ExecuteAsync(string prompt)
    {
        var mockData = $$"""
        {
          "action": "processed",
          "message": "Generic business action handled for prompt",
          "prompt_excerpt": "{{prompt[..Math.Min(80, prompt.Length)].Replace("\"", "'")}}",
          "timestamp": "{{DateTimeOffset.UtcNow:O}}"
        }
        """;
        return Task.FromResult(mockData);
    }
}
