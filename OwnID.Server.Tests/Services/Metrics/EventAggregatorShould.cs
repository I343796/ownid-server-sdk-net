using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OwnID.Server.Services.Metrics;
using Xunit;

namespace OwnID.Server.Tests.Services.Metrics
{
    public class EventAggregatorShould
    {
        private readonly EventAggregator _sut;
        private readonly DateTime _time;
        
        public EventAggregatorShould()
        {
            _time = DateTime.Now;

            _sut = new EventAggregator();
        }

        [Fact]
        public void BeThreadSafe()
        {
            // Arrange
            var numberOfEvents = 100;


            var tasks = new List<Task>(numberOfEvents);
            var results = new List<IEnumerable<(Event Event, int Count)>>();


            // Act
            for (var i = 0; i < numberOfEvents; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => _sut.AddEvent("namespace", "event name", _time)));

                if (i % 10 == 9)
                {
                    results.Add(_sut.PopAllEvents());
                }
            }

            Task.WaitAll(tasks.ToArray());

            results.Add(_sut.PopAllEvents());


            // Assert
            Assert.Equal(numberOfEvents, results.Sum(x => x.Sum(y => y.Count)));
        }

        [Fact]
        public void ReturnCorrectEventsCount()
        {
            for (var i = 0; i < 100; i++)
            {
                _sut.AddEvent("namespace", "event name", _time);
            }
            
            Assert.Equal(100, _sut.EventsCount);
        }
    }
}