﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Caliburn.Micro;
using RDCManager.Converters;
using RDCManager.Messages;
using RDCManager.Models;

namespace RDCManager.ViewModels
{
    public class RDCCollectionViewModel : Screen
    {
        private ObservableCollection<RDC> _rdcs;
        public ObservableCollection<RDC> RDCs
        {
            get { return _rdcs; }
            private set { _rdcs = value; NotifyOfPropertyChange(() => RDCs); }
        }

        private RDC _selectedRDC;
        public RDC SelectedRDC
        {
            get { return _selectedRDC; }
            set { _selectedRDC = value; NotifyOfPropertyChange(() => SelectedRDC); }
        }

        private ObservableCollection<RDCGroup> _rdcGroups;
        public ObservableCollection<RDCGroup> RDCGroups
        {
            get { return _rdcGroups; }
            private set { _rdcGroups = value; NotifyOfPropertyChange(() => RDCGroups); }
        }

        private RDCGroup _selectedRDCGroup;
        public RDCGroup SelectedRDCGroup
        {
            get { return _selectedRDCGroup; }
            set { _selectedRDCGroup = value; _groupView?.Refresh(); NotifyOfPropertyChange(() => SelectedRDCGroup); }
        }

        private readonly IEventAggregator _events;
        private readonly IRDCInstanceManager _rdcInstanceManager;
        private readonly IRDCGroupManager _rdcGroupManager;

        private ICollectionView _groupView;

        public RDCCollectionViewModel(IEventAggregator events, IRDCInstanceManager rdcInstanceManager, IRDCGroupManager rdcGroupManager)
        {
            _events = events;
            _rdcInstanceManager = rdcInstanceManager;
            _rdcGroupManager = rdcGroupManager;
        }

        protected override void OnActivate()
        {
            RDCGroups = new ObservableCollection<RDCGroup>(_rdcGroupManager.GetGroups());
            RDCGroups.Insert(0, new RDCGroup() { Name = "All", Id = Guid.NewGuid() });
            RDCGroups.Insert(1, new RDCGroup() { Name = "None", Id = Guid.Empty });

            var rdcs = _rdcInstanceManager.GetRDCs()
                                          .OrderBy(x => _rdcGroups.FirstOrDefault(y => y.Id == x.GroupId)?.Name ?? "None");

            RDCs = new ObservableCollection<RDC>(rdcs);

            SelectedRDCGroup = RDCGroups.First();

            _groupView = CollectionViewSource.GetDefaultView(_rdcs);
            _groupView.GroupDescriptions.Add(new PropertyGroupDescription("GroupId", new GroupIdToNameConverter(_rdcGroups)));
            _groupView.Filter += (item) =>
            {
                RDC rdc = item as RDC;

                if (!_rdcGroups.Any(x => x.Id == rdc.GroupId))
                {
                    rdc.GroupId = Guid.Empty;
                }

                if ((rdc.GroupId == Guid.Empty && SelectedRDCGroup.Name == "None") ||
                    SelectedRDCGroup.Name == "All" ||
                    rdc.GroupId == SelectedRDCGroup.Id)
                {
                    return true;
                }

                return false;
            };
        }

        protected override void OnDeactivate(bool close)
        {
            _groupView.Filter = null;

            base.OnDeactivate(close);
        }

        public void RDCSelected()
        {
            foreach(RDC rdc in RDCs)
            {
                rdc.IsSelected = rdc == SelectedRDC;
            }

            _events.PublishOnUIThread(new RDCSelectedMessage() { SelectedRDC = SelectedRDC });
            _events.PublishOnUIThread(new SwitchViewMessage() { View = Models.View.Session });
        }

        public void NewRDC()
        {
            RDC rdc = _rdcInstanceManager.CreateNew();

            RDCs.Add(rdc);

            SelectedRDC = rdc;

            RDCSelected();
        }
    }
}