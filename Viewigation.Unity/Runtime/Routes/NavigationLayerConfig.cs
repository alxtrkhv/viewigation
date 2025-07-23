using System;
using System.Collections.Generic;
using UnityEngine;
using Viewigation.Blocks;

namespace Viewigation.Routes
{
  [Serializable]
  public class NavigationLayerConfig : INavigationLayerConfig
  {
    [field: SerializeField]
    public string Id { get; private set; } = null!;

    [field: SerializeField]
    public bool UnloadViewOnClose { get; private set; }

    [field: SerializeField]
    public Transform? Root { get; private set; }

    [SerializeField]
    private Transform? _pool;

    [SerializeField]
    private BaseInputBlocker? _blocker;

    [field: SerializeField]
    public List<string> DependentLayers { get; private set; } = null!;

    public Transform? Pool
    {
      get => _pool ??= Root;
      private set => _pool = value;
    }

    public IBlockWithOverride? InputBlocker => _blocker;

    public INavigationLayerConfig Self => this;
    public List<IRoute> LoadedRoutes { get; } = new();
    public List<IRoute> ActiveRoutes { get; } = new();
  }
}
