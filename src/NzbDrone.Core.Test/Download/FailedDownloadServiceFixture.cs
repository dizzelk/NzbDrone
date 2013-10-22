﻿using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Tv;

namespace NzbDrone.Core.Test.Download
{
    [TestFixture]
    public class FailedDownloadServiceFixture : CoreTest<FailedDownloadService>
    {
        private Series _series;
        private Episode _episode;
        private List<HistoryItem> _completed;
        private List<HistoryItem> _failed;

        [SetUp]
        public void Setup()
        {
            _series = Builder<Series>.CreateNew().Build();
            _episode = Builder<Episode>.CreateNew().Build();

            _completed = Builder<HistoryItem>.CreateListOfSize(5)
                                             .All()
                                             .With(h => h.Status = HistoryStatus.Completed)
                                             .Build()
                                             .ToList();

            _failed = Builder<HistoryItem>.CreateListOfSize(1)
                                          .All()
                                          .With(h => h.Status = HistoryStatus.Failed)
                                          .Build()
                                          .ToList();

            Mocker.GetMock<IProvideDownloadClient>()
                  .Setup(c => c.GetDownloadClient()).Returns(Mocker.GetMock<IDownloadClient>().Object);
        }

        private void GivenNoRecentHistory()
        {
            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.BetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>(), HistoryEventType.Grabbed))
                  .Returns(new List<History.History>());
        }

        private void GivenRecentHistory(List<History.History> history)
        {
            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.BetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>(), HistoryEventType.Grabbed))
                  .Returns(history);
        }

        private void GivenNoFailedHistory()
        {
            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.Failed())
                  .Returns(new List<History.History>());
        }

        private void GivenFailedHistory(List<History.History> failedHistory)
        {
            Mocker.GetMock<IHistoryService>()
                  .Setup(s => s.Failed())
                  .Returns(failedHistory);
        }

        private void GivenFailedDownloadClientHistory()
        {
            Mocker.GetMock<IDownloadClient>()
                  .Setup(s => s.GetHistory(0, 20))
                  .Returns(_failed);
        }

        private void VerifyNoFailedDownloads()
        {
            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.IsAny<DownloadFailedEvent>()), Times.Never());
        }

        private void VerifyFailedDownloads(int count = 1)
        {
            Mocker.GetMock<IEventAggregator>()
                .Verify(v => v.PublishEvent(It.IsAny<DownloadFailedEvent>()), Times.Exactly(count));
        }

        [Test]
        public void should_not_process_if_no_download_client_history()
        {
            Mocker.GetMock<IDownloadClient>()
                  .Setup(s => s.GetHistory(0, 20))
                  .Returns(new List<HistoryItem>());

            Subject.Execute(new FailedDownloadCommand());

            Mocker.GetMock<IHistoryService>()
                  .Verify(s => s.BetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>(), HistoryEventType.Grabbed),
                      Times.Never());

            VerifyNoFailedDownloads();
        }

        [Test]
        public void should_not_process_if_no_failed_items_in_download_client_history()
        {
            Mocker.GetMock<IDownloadClient>()
                  .Setup(s => s.GetHistory(0, 20))
                  .Returns(_completed);

            Subject.Execute(new FailedDownloadCommand());

            Mocker.GetMock<IHistoryService>()
                  .Verify(s => s.BetweenDates(It.IsAny<DateTime>(), It.IsAny<DateTime>(), HistoryEventType.Grabbed),
                      Times.Never());

            VerifyNoFailedDownloads();
        }

        [Test]
        public void should_not_process_if_matching_history_is_not_found()
        {
            GivenNoRecentHistory();
            GivenFailedDownloadClientHistory();

            Subject.Execute(new FailedDownloadCommand());

            VerifyNoFailedDownloads();
        }

        [Test]
        public void should_not_process_if_already_added_to_history_as_failed()
        {
            GivenFailedDownloadClientHistory();
            
            var history = Builder<History.History>.CreateListOfSize(1)
                                                  .Build()
                                                  .ToList();
            
            GivenRecentHistory(history);
            GivenFailedHistory(history);

            history.First().Data.Add("downloadClient", "SabnzbdClient");
            history.First().Data.Add("downloadClientId", _failed.First().Id);

            Subject.Execute(new FailedDownloadCommand());

            VerifyNoFailedDownloads();
        }

        [Test]
        public void should_process_if_not_already_in_failed_history()
        {
            GivenFailedDownloadClientHistory();

            var history = Builder<History.History>.CreateListOfSize(1)
                                                  .Build()
                                                  .ToList();

            GivenRecentHistory(history);
            GivenNoFailedHistory();

            history.First().Data.Add("downloadClient", "SabnzbdClient");
            history.First().Data.Add("downloadClientId", _failed.First().Id);

            Subject.Execute(new FailedDownloadCommand());

            VerifyFailedDownloads();
        }

        [Test]
        public void should_process_for_each_failed_episode()
        {
            GivenFailedDownloadClientHistory();

            var history = Builder<History.History>.CreateListOfSize(2)
                                                  .Build()
                                                  .ToList();

            GivenRecentHistory(history);
            GivenNoFailedHistory();

            history.ForEach(h =>
            {
                h.Data.Add("downloadClient", "SabnzbdClient");
                h.Data.Add("downloadClientId", _failed.First().Id);
            });

            Subject.Execute(new FailedDownloadCommand());

            VerifyFailedDownloads(2);
        }
    }
}