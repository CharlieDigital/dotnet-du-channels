using OneOf;

namespace DotnetDuChannels;

public record UrgencyScore();

public record RoutingTicket();

public record KeywordTags();

[GenerateOneOf]
public partial class CallLogFacet : OneOfBase<UrgencyScore, RoutingTicket, KeywordTags> { }

public class CallLog
{
  public UrgencyScore? Urgency { get; set; }

  public RoutingTicket? Routing { get; set; }

  public KeywordTags? Keywords { get; set; }

  public bool IsReady => Urgency != null && Routing != null && Keywords != null;

  public async Task RouteAsync()
  {
    // TODO: Actual routing here.
    await Task.CompletedTask;
  }
}
