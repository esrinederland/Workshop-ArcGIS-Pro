using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading;
using ArcGIS.Desktop.Core;
using Newtonsoft.Json.Linq;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;

namespace MyContentAddIn
{
    internal class MyContentDockpaneViewModel : DockPane
    {
        private const string _dockPaneID = "MyContentAddIn_MyContentDockpane";

        protected MyContentDockpaneViewModel()
        {
            AddLayerCommand = new RelayCommand((listItem) => AddLayerToMap(listItem), () => true);

            LoadMyContent();
        }

        /// <summary>
        /// Text shown near the top of the DockPane.
        /// </summary>
        private string _heading = "My Content";
        public string Heading
        {
            get { return _heading; }
            set
            {
                SetProperty(ref _heading, value, () => Heading);
            }
        }

        /// <summary>
        /// Collection containing the Portal items.
        /// </summary>
        private ObservableCollection<Item> _items = new ObservableCollection<Item>();
        public ObservableCollection<Item> Items
        {
            get
            {
                return _items;
            }
            set
            {
                SetProperty(ref _items, value, () => Items);
            }
        }
        
        /// <summary>
        /// Command for adding a layer to the active map.
        /// </summary>
        public RelayCommand AddLayerCommand { get; private set; }

        /// <summary>
        /// Show the DockPane.
        /// </summary>
        internal static void Show()
        {
            DockPane pane = FrameworkApplication.DockPaneManager.Find(_dockPaneID);
            if (pane == null)
                return;

            pane.Activate();
        }

        /// <summary>
        /// Create a Layer from the selected item and add it to the active map.
        /// </summary>
        /// <param name="listItem">Selected item(ArcGIS.Desktop.Core.Item)</param>
        private void AddLayerToMap(object listItem)
        {
            QueuedTask.Run(() =>
            {
                var selectedItem = listItem as Item;

                if (selectedItem != null && MapView.Active != null)
                {
                    Layer newLayer = LayerFactory.CreateLayer(selectedItem, MapView.Active.Map);

                    MapView.Active.ZoomTo(newLayer.QueryExtent(), new TimeSpan(0, 0, 0, 0, 500));
                }
            });
        }

        /// <summary>
        /// Query the portal and load all the content items to a list.
        /// </summary>
        private async void LoadMyContent()
        {
            try
            {
                UriBuilder searchURL = new UriBuilder(ArcGISPortalManager.Current.GetActivePortal().PortalUri);
                searchURL.Path = "sharing/rest/search";
                searchURL.Query = string.Format("q=owner:\"{0}\"&num=100&f=json", ArcGISPortalManager.Current.GetActivePortal().GetSignOnUsername());

                EsriHttpClient httpClient = new EsriHttpClient();

                var searchResponse = httpClient.Get(searchURL.Uri.ToString());

                dynamic resultItems = JObject.Parse(await searchResponse.Content.ReadAsStringAsync());

                if (resultItems.total.Value == 0) return;

                List<dynamic> resultItemList = new List<dynamic>();
                resultItemList.AddRange(resultItems.results);

                foreach (var item in resultItemList)
                {
                    string itemID = item.id;

                    Item currentItem = ItemFactory.Create(itemID, ItemFactory.ItemType.PortalItem);
                    
                    if (LayerFactory.CanCreateLayerFrom(currentItem))
                    {
                        Items.Add(currentItem);
                    }
                }
            }
            catch (Exception)
            {
                // handle exception
            }
        }
    }

    /// <summary>
    /// Button implementation to show the DockPane.
    /// </summary>
    internal class MyContentDockpane_ShowButton : Button
    {
        protected override void OnClick()
        {
            MyContentDockpaneViewModel.Show();
        }
    }
}
