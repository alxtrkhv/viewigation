using System.Collections.Generic;
using UnityEngine;
using Viewigation.Blocks;

namespace Viewigation.Routes
{
  public interface INavigationLayerConfig
  {
    public string Id { get; }
    public bool UnloadViewOnClose { get; }

    public Transform? Root { get; }
    public Transform? Pool { get; }
    public IBlockWithOverride? InputBlocker { get; }

    public INavigationLayerConfig Self { get; }

    public List<IRoute> LoadedRoutes { get; }
    public List<IRoute> ActiveRoutes { get; }

    public List<string> DependentLayers { get; }
  }
}
