using System.Threading.Channels;
using DotnetDuChannels;

var channel = Channel.CreateUnbounded<(int, CallLogFacet)>();

// The reader side of the channel which aggregates the facets and then sends
// off the call log for routing.
var aggregator = async (ChannelReader<(int RecordId, CallLogFacet Facet)> reader) =>
{
  var state = new Dictionary<int, CallLog>();

  while (await reader.WaitToReadAsync())
  {
    if (!reader.TryRead(out var part))
    {
      continue;
    }

    if (!state.ContainsKey(part.RecordId))
    {
      state[part.RecordId] = new CallLog();
    }

    var log = state[part.RecordId];

    part.Facet.Switch(
      urgency => log.Urgency = urgency,
      routing => log.Routing = routing,
      keywords => log.Keywords = keywords
    );

    if (log.IsReady)
    {
      Console.WriteLine($"Call record {part.RecordId} ready; routing...");

      await log.RouteAsync();
    }
  }
};

var urgencyProducer = async (ChannelWriter<(int, CallLogFacet)> writer, int callRecord) =>
{
  // TODO: Actual logic here to compute urgency
  await writer.WriteAsync((callRecord, new UrgencyScore()));
};

var routingProducer = async (ChannelWriter<(int, CallLogFacet)> writer, int callRecord) =>
{
  // TODO: Actual logic here to determine routing
  await writer.WriteAsync((callRecord, new RoutingTicket()));
};

var keywordsProducer = async (ChannelWriter<(int, CallLogFacet)> writer, int callRecord) =>
{
  // TODO: Actual logic here to extract keywords
  await writer.WriteAsync((callRecord, new KeywordTags()));
};

var aggregatorTask = aggregator(channel.Reader);

// Our producer yields a series of async tasks.  Each task processes one facet of the
// call that we want to produce metadata for.
var producerTasks = Enumerable
  .Range(0, 20)
  .AsParallel()
  .WithDegreeOfParallelism(10)
  .SelectMany(i =>
  {
    return new Task[]
    {
      urgencyProducer(channel.Writer, i),
      routingProducer(channel.Writer, i),
      keywordsProducer(channel.Writer, i)
    };
  });

Task.WaitAll([.. producerTasks]);

channel.Writer.Complete();

await aggregatorTask;
