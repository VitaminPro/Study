//До рефакторинга
public class Plane
{
  private int FlightId;
  private Person Pilot;
  private int _currentNumber = 0;
  private string coordinate;
  public Plane(Person p, int id)
  {
    Pilot = p;
    FlightId = id;
    Passengers = new ConcurrentDictionary<int,Person>();
  }
  
  public void Registor(Person p)
  {
     Passengers[_currentNumber++] = p;
  }
  
  private static object _sync = new object();
  
  public async void CheckCoordinate()
  {
	  Monitor.Enter(_sync);
	  try
	  {
          string temp = await Navigator.GetCoordinate();
          if(!coordinate.Equals(temp))
          {
               coordinate = temp
		      }			  
	  }
      catch (Exception e)
      {
          Monitor.Exit(_sync);
      }		  	  
  }	
   
   public void ChangePilot(string fn,string ln, string doc)
   {
      Pilot.FirstName = fn;
      Pilot.LastName = ln;	 
   }
   
   public async void StartFlight()
   {
	  await FlightDb.Start(FlightId, JsonConverter.SerializeObject(Passengers));
      await FlighKafkaQueue.SendStart(FlightId, JsonConverter.SerializeObject(Passengers)); 	  
   }
   public ConcurrentDictionary<int,Person> Passengers { get; set; }
}  
  
public class Person
{
   public string DocumentId { get; set; }
   public string FirstName { get; set;}
   public string LastName { get; set; }   		
}	


//После рефакторинга. Chat Gtp.
/*
У Plane одна ответственность — хранить и управлять своим состоянием (пилот, пассажиры, координаты).
Поэтому регистрация пассажира здесь уместна. А вот работа с БД и очередями вынесена в FlightService, чтобы не смешивать бизнес и инфраструктуру.
*/
/// <summary>
/// Доменная модель: Самолёт.
/// Хранит состояние и бизнес-логику (пассажиры, пилот, координаты).
/// Не зависит от БД и Kafka.
/// </summary>
public class Plane
{
    private readonly object _sync = new();
    private readonly List<IPerson> _passengers = new();
    private string? _coordinate;

    public int FlightId { get; }
    public IPerson Pilot { get; private set; }

    public IReadOnlyCollection<IPerson> Passengers => _passengers.AsReadOnly();

    public Plane(int flightId, IPerson pilot)
    {
        FlightId = flightId;
        Pilot = pilot ?? throw new ArgumentNullException(nameof(pilot));
    }

    /// <summary>
    /// Регистрирует пассажира (с валидацией и потокобезопасностью).
    /// </summary>
    public void RegisterPassenger(IPerson passenger)
    {
        if (passenger == null) 
            throw new ArgumentNullException(nameof(passenger));

        lock (_sync)
        {
            _passengers.Add(passenger);
        }
    }

    /// <summary>
    /// Смена пилота (например, при пересменке экипажа).
    /// </summary>
    public void ChangePilot(string firstName, string lastName, string documentId)
    {
        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Pilot name cannot be empty");

        lock (_sync)
        {
            Pilot.FirstName = firstName;
            Pilot.LastName = lastName;
            Pilot.DocumentId = documentId;
        }
    }

    /// <summary>
    /// Обновляет координаты через навигационный сервис.
    /// </summary>
    public async Task UpdateCoordinateAsync(INavigator navigator, CancellationToken token)
    {
        var newCoordinate = await navigator.GetCoordinateAsync(token);

        if (!string.Equals(_coordinate, newCoordinate, StringComparison.Ordinal))
        {
            _coordinate = newCoordinate;
        }
    }
}

/// <summary>
/// Сервис приложения: управляет запуском рейса.
/// Реализует Outbox pattern.
/// </summary>
public class FlightService
{
    private readonly AppDbContext _dbContext;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<FlightService> _logger;

    public FlightService(AppDbContext dbContext, IKafkaProducer kafkaProducer, ILogger<FlightService> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _kafkaProducer = kafkaProducer ?? throw new ArgumentNullException(nameof(kafkaProducer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Запуск рейса: сохраняем в БД и публикуем событие.
    /// </summary>
    public async Task StartFlightAsync(Plane plane, CancellationToken token)
    {
        if (plane == null) throw new ArgumentNullException(nameof(plane));

        try
        {
            // 1. Сохраняем рейс в БД
            var entity = new FlightEntity
            {
                FlightId = plane.FlightId,
                Pilot = $"{plane.Pilot.FirstName} {plane.Pilot.LastName}",
                PassengersJson = JsonConvert.SerializeObject(plane.Passengers),
                StartedAt = DateTime.UtcNow
            };

            _dbContext.Flights.Add(entity);

            // 2. Сохраняем событие в Outbox
            var outboxEvent = new OutboxEvent
            {
                EventType = "FlightStarted",
                Payload = JsonConvert.SerializeObject(new
                {
                    plane.FlightId,
                    Passengers = plane.Passengers
                }),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Outbox.Add(outboxEvent);

            await _dbContext.SaveChangesAsync(token);

            // 3. Публикуем в Kafka (или в фоне отдельным воркером)
            await _kafkaProducer.SendAsync("flights", outboxEvent.Payload, token);

            _logger.LogInformation("Flight {FlightId} started successfully", plane.FlightId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while starting flight {FlightId}", plane.FlightId);
            throw; // Пробрасываем дальше
        }
    }
}

//Интерфейсы и вспомогательные классы
public interface IPerson
{
    string DocumentId { get; set; }
    string FirstName { get; set; }
    string LastName { get; set; }
}

public class Person : IPerson
{
    public string DocumentId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
}

/// <summary>
/// Навигационный сервис.
/// </summary>
public interface INavigator
{
    Task<string> GetCoordinateAsync(CancellationToken token);
}

/// <summary>
/// Kafka-паблишер (интерфейс для тестируемости).
/// </summary>
public interface IKafkaProducer
{
    Task SendAsync(string topic, string message, CancellationToken token);
}

/// <summary>
/// Сущность Flight в БД.
/// </summary>
public class FlightEntity
{
    public int Id { get; set; }
    public int FlightId { get; set; }
    public string Pilot { get; set; } = string.Empty;
    public string PassengersJson { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
}

/// <summary>
/// Outbox-таблица для событий.
/// </summary>
public class OutboxEvent
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
