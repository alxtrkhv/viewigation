using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NSubstitute;
using NSubstitute.Core;
using NUnit.Framework;
using Viewigation.Blocks;
using Viewigation.Navigation;
using Viewigation.Routes;
using Viewigation.Views;

namespace Viewigation.Tests.Tests.Routes
{
  [TestFixture]
  public partial class NavigationLayerTests
  {
    private INavigationLayer _navigationLayer;
    private IView<string> _dummyViewWithParameters;

    private IRouteFactory _routeFactory;
    private INavigationLayerConfig _navigationLayerConfig;
    private readonly List<IRoute> _activeRoutes = new();
    private readonly List<IRoute> _loadedRoutes = new();
    private readonly IBlockWithOverride _inputBlock = new BlockWithOverride(true);

    [OneTimeSetUp]
    public void InitOneTime()
    {
      _navigationLayerConfig = Substitute.For<INavigationLayerConfig>();
      _navigationLayerConfig.ActiveRoutes.Returns(_activeRoutes);
      _navigationLayerConfig.LoadedRoutes.Returns(_loadedRoutes);

      _navigationLayerConfig.InputBlocker.Returns(_inputBlock);

      _routeFactory = Substitute.For<IRouteFactory>();

      _routeFactory
        .NewRoute<DummyUnityRouteWithParameters>(Arg.Any<string>())
        .Returns(NewRoute<DummyUnityRouteWithParameters>);

      _routeFactory
        .NewView(Arg.Any<DummyUnityRouteWithParameters>(), _navigationLayerConfig)
        .Returns(_ => new(_dummyViewWithParameters));

      _routeFactory
        .NewRoute<RouteWithoutValidView>(Arg.Any<string>())
        .Returns(NewRoute<RouteWithoutValidView>);

      _routeFactory
        .NewView(Arg.Any<RouteWithoutValidView>(), _navigationLayerConfig)
        .Returns(_ => new(null));

      _routeFactory
        .NewRoute<SlowRoute>(Arg.Any<string>())
        .Returns(NewRoute<SlowRoute>);

      _routeFactory
        .NewView(Arg.Any<SlowRoute>(), _navigationLayerConfig)
        .Returns(_ => new(new SlowView()));

      _routeFactory
        .NewRoute<NonSuspensiveUnityRoute>(Arg.Any<string>())
        .Returns(NewRoute<NonSuspensiveUnityRoute>);

      _routeFactory
        .NewView(Arg.Any<NonSuspensiveUnityRoute>(), _navigationLayerConfig)
        .Returns(_ => new(Substitute.For<IView>()));

      _routeFactory
        .NewRoute<SuspensiveUnityRoute>(Arg.Any<string>())
        .Returns(NewRoute<SuspensiveUnityRoute>);

      _routeFactory
        .NewView(Arg.Any<SuspensiveUnityRoute>(), _navigationLayerConfig)
        .Returns(_ => new(Substitute.For<IView>()));

      _routeFactory
        .ExistingView(Arg.Any<IRoute>(), _navigationLayerConfig)
        .Returns(_ => null);

      return;

      TRoute NewRoute<TRoute>(CallInfo callInfo) where TRoute : IRoute, new()
      {
        var newRoute = new TRoute();

        var castedRoute = (IRoute)newRoute;
        castedRoute.Id = callInfo.Arg<string>();

        return newRoute;
      }
    }

    [SetUp]
    public void Init()
    {
      _navigationLayer = new NavigationLayer(
        _navigationLayerConfig,
        _routeFactory
      );

      _dummyViewWithParameters = new ViewWithParameters<string>();
    }

    [TearDown]
    public void TearDown()
    {
      _navigationLayerConfig.InputBlocker?.Flush();

      _activeRoutes.Clear();
      _loadedRoutes.Clear();
      _inputBlock.Flush();
    }

    [Test]
    public async Task OpeningSuspensiveRoute_SuspendsAllOtherRoutes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "3");

      await _navigationLayer.Open<SuspensiveUnityRoute>();

      Assert.That(_navigationLayer.Stack[0].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[1].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[2].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[3].State, Is.EqualTo(RouteState.Open));
    }

    [Test]
    public async Task OpeningSuspensiveRoute_SuspendsOtherSuspensiveRoutes()
    {
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "3");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "4");

      Assert.That(_navigationLayer.Stack[0].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[1].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[2].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[3].State, Is.EqualTo(RouteState.Open));
    }

    [Test]
    public async Task ClosingSuspensiveRoute_ResumesOtherSuspensiveRoute()
    {
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "3");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "4");

      await _navigationLayer.Close<SuspensiveUnityRoute>(id: "4");

      Assert.That(_navigationLayer.Stack[0].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[1].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[2].State, Is.EqualTo(RouteState.Open));
    }

    [Test]
    public async Task ClosingSuspensiveRoute_ResumesAllOtherRoutes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "3");
      await _navigationLayer.Open<SuspensiveUnityRoute>();

      await _navigationLayer.Close<SuspensiveUnityRoute>();

      Assert.That(_navigationLayer.Stack[0].State, Is.EqualTo(RouteState.Open));
      Assert.That(_navigationLayer.Stack[1].State, Is.EqualTo(RouteState.Open));
      Assert.That(_navigationLayer.Stack[2].State, Is.EqualTo(RouteState.Open));
    }

    [Test]
    public async Task OpeningNonSuspensiveRoute_DoesNotSuspendOtherScreen()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "2");

      Assert.That(_navigationLayer.Stack[0].State, Is.EqualTo(RouteState.Open));
      Assert.That(_navigationLayer.Stack[1].State, Is.EqualTo(RouteState.Open));
    }

    [Test]
    public async Task OpeningRouteWithSameId_DoesNotAddToStack()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      Assert.That(_navigationLayer.Stack.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task OpeningRouteWithDifferentId_AddsToStack()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      Assert.That(_navigationLayer.Stack.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Suspension_SuspendsAllRoutes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "3");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "4");

      _navigationLayer.Suspend();

      Assert.That(_navigationLayer.Stack.All(x => x.IsSuspended), Is.EqualTo(true));
    }

    [Test]
    public async Task Resume_ResumesAllRoutes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "3");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "4");

      _navigationLayer.Suspend();
      _navigationLayer.Resume();

      Assert.That(_navigationLayer.Stack.All(x => x.State == RouteState.Open), Is.True);
      Assert.That(_navigationLayer.Stack.All(x => x.IsSuspended), Is.False);
    }

    [Test]
    public async Task Resume_ResumesResumableRoutesOnly()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "2");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "3");
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "4");

      _navigationLayer.Suspend();
      _navigationLayer.Resume();

      Assert.That(_navigationLayer.Stack[0].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[1].IsSuspended, Is.True);
      Assert.That(_navigationLayer.Stack[2].State, Is.EqualTo(RouteState.Open));
      Assert.That(_navigationLayer.Stack[3].State, Is.EqualTo(RouteState.Open));
      Assert.That(_navigationLayer.Stack[4].State, Is.EqualTo(RouteState.Open));
    }

    [Test]
    public async Task OpeningValidRoute_ReturnsRouteInstance()
    {
      var route = await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task OpeningValidRouteWithParameters_ReturnsRouteInstance()
    {
      var route = await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: string.Empty);

      Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task ReopeningValidRoute_ReturnsRouteInstance()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      var route = await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task ReopeningValidRouteWithParameters_ReturnsRouteInstance()
    {
      await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: string.Empty);

      var route = await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: string.Empty);

      Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task LoadingValidRoute_ReturnsRouteInstance()
    {
      var route = await _navigationLayer.Load<NonSuspensiveUnityRoute>();

      Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task ReloadingValidRoute_ReturnsRouteInstance()
    {
      await _navigationLayer.Load<NonSuspensiveUnityRoute>();

      var route = await _navigationLayer.Load<NonSuspensiveUnityRoute>();

      Assert.That(route, Is.Not.Null);
    }

    [Test]
    public async Task Opening_SetsParameters()
    {
      var parameters = "PARAMETERS";
      var route = await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: parameters);

      Assert.That(route.View.Parameters, Is.EqualTo(parameters));
    }

    [Test]
    public async Task Reopening_WontSetParameters()
    {
      var parameters = "PARAMETERS";
      var parameters2 = "PARAMETERS_TWO";

      var route = await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: parameters);
      await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: parameters2);

      Assert.That(route.View.Parameters, Is.EqualTo(parameters));
    }

    [Test]
    public async Task Closing_UnsetsParameters()
    {
      var parameters = "PARAMETERS";

      await _navigationLayer.Open<DummyUnityRouteWithParameters, string>(parameters: parameters);
      await _navigationLayer.Close<DummyUnityRouteWithParameters>();

      Assert.That(_dummyViewWithParameters.Parameters, Is.EqualTo(default(string)));
    }

    [Test]
    public void SuspensionWhenEmpty_DontThrow()
    {
      Assert.DoesNotThrow(() => _navigationLayer.Suspend());
    }

    [Test]
    public void ResumeWhenEmpty_DontThrow()
    {
      Assert.DoesNotThrow(() => _navigationLayer.Resume());
    }

    [Test]
    public async Task Opening_TemporarilyBlocksInput()
    {
      var parameters = new SlowViewParameters();
      var task = _navigationLayer.Open<SlowRoute, SlowViewParameters>(parameters: parameters);

      var blockedWhile = _navigationLayerConfig.InputBlocker?.IsActive == true;
      parameters.ShowTcs.TrySetResult();
      await task;
      var notBlockedAfter = _navigationLayerConfig.InputBlocker?.IsActive == false;

      Assert.That(blockedWhile, Is.True);
      Assert.That(notBlockedAfter, Is.True);
    }

    [Test]
    public async Task Closing_TemporarilyBlocksInput()
    {
      var parameters = new SlowViewParameters();
      parameters.ShowTcs.TrySetResult();
      await _navigationLayer.Open<SlowRoute, SlowViewParameters>(parameters: parameters);
      var task = _navigationLayer.Close<SlowRoute>();

      var blockedWhile = _navigationLayerConfig.InputBlocker?.IsActive == true;
      parameters.HideTcs.TrySetResult();
      await task;
      var notBlockedAfter = _navigationLayerConfig.InputBlocker?.IsActive == false;

      Assert.That(blockedWhile, Is.True);
      Assert.That(notBlockedAfter, Is.True);
    }

    [Test]
    public async Task Suspension_BlocksInput()
    {
      var parameters = new SlowViewParameters();
      parameters.ShowTcs.TrySetResult();
      await _navigationLayer.Open<SlowRoute, SlowViewParameters>(parameters: parameters);

      var notBlockedBefore = _navigationLayerConfig.InputBlocker?.IsActive == false;
      parameters.SuspendTcs.TrySetResult();
      _navigationLayer.Suspend();
      var blockedAfter = _navigationLayerConfig.InputBlocker?.IsActive == true;

      Assert.That(notBlockedBefore, Is.True);
      Assert.That(blockedAfter, Is.True);
    }

    [Test]
    public async Task Resume_UnblocksInput()
    {
      var parameters = new SlowViewParameters();
      parameters.ShowTcs.TrySetResult();
      parameters.SuspendTcs.TrySetResult();
      await _navigationLayer.Open<SlowRoute, SlowViewParameters>(parameters: parameters);
      _navigationLayer.Suspend();

      var blockedBefore = _navigationLayerConfig.InputBlocker?.IsActive == true;
      parameters.ResumeTcs.TrySetResult();
      _navigationLayer.Resume();
      var notBlockedAfter = _navigationLayerConfig.InputBlocker?.IsActive == false;

      Assert.That(blockedBefore, Is.True);
      Assert.That(notBlockedAfter, Is.True);
    }

    [Test]
    public async Task OpeningInvalidRoute_UnblocksInputAfter()
    {
      await _navigationLayer.Open<RouteWithoutValidView>();
      var notBlockedAfter = _navigationLayerConfig.InputBlocker?.IsActive == false;

      Assert.That(notBlockedAfter, Is.True);
    }

    [Test]
    public async Task ClosingInvalidRoute_UnblocksInputAfter()
    {
      await _navigationLayer.Open<RouteWithoutValidView>();
      await _navigationLayer.Close<RouteWithoutValidView>();

      var notBlockedAfter = _navigationLayerConfig.InputBlocker?.IsActive == false;

      Assert.That(notBlockedAfter, Is.True);
    }

    [Test]
    public async Task MultipleSuspensions_ResumesAfterSameNumberOfResumes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      _navigationLayer.Suspend();
      _navigationLayer.Suspend();
      _navigationLayer.Suspend();

      _navigationLayer.Resume();
      _navigationLayer.Resume();
      _navigationLayer.Resume();

      Assert.That(_navigationLayer.Stack.All(x => x.State == RouteState.Open), Is.EqualTo(true));
    }

    [Test]
    public async Task MultipleSuspensions_ResumedWhenResumeCountExceedsSuspendCount()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      _navigationLayer.Suspend();
      _navigationLayer.Suspend();

      _navigationLayer.Resume();
      _navigationLayer.Resume();
      _navigationLayer.Resume();

      Assert.That(_navigationLayer.Stack.All(x => x.State == RouteState.Open), Is.EqualTo(true));
    }

    [Test]
    public async Task MultipleSuspensions_CanBeSuspendedAgainAfterExtraResumes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      _navigationLayer.Suspend();
      _navigationLayer.Suspend();

      _navigationLayer.Resume();
      _navigationLayer.Resume();
      _navigationLayer.Resume();

      _navigationLayer.Suspend();

      Assert.That(_navigationLayer.Stack.All(x => x.IsSuspended), Is.EqualTo(true));
    }

    [Test]
    public async Task MultipleSuspensions_CanBeSuspendedAndResumedAgainAfterExtraResumes()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      _navigationLayer.Suspend();
      _navigationLayer.Suspend();

      _navigationLayer.Resume();
      _navigationLayer.Resume();
      _navigationLayer.Resume();

      _navigationLayer.Suspend();
      _navigationLayer.Suspend();

      _navigationLayer.Resume();
      _navigationLayer.Resume();

      Assert.That(_navigationLayer.Stack.All(x => x.State == RouteState.Open), Is.EqualTo(true));
    }

    [Test]
    public async Task Close_WithUnloadViewOnCloseTrue_UnloadsRouteByDefault()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(true);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      var loadedCountBefore = _navigationLayerConfig.LoadedRoutes.Count;

      await _navigationLayer.Close<NonSuspensiveUnityRoute>();
      var loadedCountAfter = _navigationLayerConfig.LoadedRoutes.Count;

      Assert.That(loadedCountBefore, Is.EqualTo(1));
      Assert.That(loadedCountAfter, Is.EqualTo(0));
    }

    [Test]
    public async Task Close_WithUnloadViewOnCloseFalse_DoesNotUnloadRouteByDefault()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(false);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      var loadedCountBefore = _navigationLayerConfig.LoadedRoutes.Count;

      await _navigationLayer.Close<NonSuspensiveUnityRoute>();
      var loadedCountAfter = _navigationLayerConfig.LoadedRoutes.Count;

      Assert.That(loadedCountBefore, Is.EqualTo(1));
      Assert.That(loadedCountAfter, Is.EqualTo(1));
    }

    [Test]
    public async Task Close_WithUnloadTrueOverridesUnloadViewOnCloseFalse_UnloadsRoute()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(false);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      var loadedCountBefore = _navigationLayerConfig.LoadedRoutes.Count;

      await _navigationLayer.Close<NonSuspensiveUnityRoute>(unload: true);
      var loadedCountAfter = _navigationLayerConfig.LoadedRoutes.Count;

      Assert.That(loadedCountBefore, Is.EqualTo(1));
      Assert.That(loadedCountAfter, Is.EqualTo(0));
    }

    [Test]
    public async Task Close_WithUnloadFalseOverridesUnloadViewOnCloseTrue_DoesNotUnloadRoute()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(true);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      var loadedCountBefore = _navigationLayerConfig.LoadedRoutes.Count;

      await _navigationLayer.Close<NonSuspensiveUnityRoute>(unload: false);
      var loadedCountAfter = _navigationLayerConfig.LoadedRoutes.Count;

      Assert.That(loadedCountBefore, Is.EqualTo(1));
      Assert.That(loadedCountAfter, Is.EqualTo(1));
    }

    [Test]
    public async Task Close_WithUnloadViewOnCloseTrue_ReturnsNullWhenUnloaded()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(true);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      var result = await _navigationLayer.Close<NonSuspensiveUnityRoute>();

      Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Close_WithUnloadViewOnCloseFalse_ReturnsRouteWhenNotUnloaded()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(false);

      var openedRoute = await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      var result = await _navigationLayer.Close<NonSuspensiveUnityRoute>();

      Assert.That(result, Is.EqualTo(openedRoute));
    }

    [Test]
    public async Task Close_WithUnloadTrue_ReturnsNullRegardlessOfUnloadViewOnClose()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(false);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      var result = await _navigationLayer.Close<NonSuspensiveUnityRoute>(unload: true);

      Assert.That(result, Is.Null);
    }

    [Test]
    public async Task Close_WithUnloadFalse_ReturnsRouteRegardlessOfUnloadViewOnClose()
    {
      _navigationLayerConfig.UnloadViewOnClose.Returns(true);

      var openedRoute = await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      var result = await _navigationLayer.Close<NonSuspensiveUnityRoute>(unload: false);

      Assert.That(result, Is.EqualTo(openedRoute));
    }

    [Test]
    public void Suspend_SuspendsDependentLayers()
    {
      var dependentLayer1 = Substitute.For<INavigationLayer>();
      var dependentLayer2 = Substitute.For<INavigationLayer>();

      var navigation = Substitute.For<INavigation>();
      navigation["dependent1"].Returns(dependentLayer1);
      navigation["dependent2"].Returns(dependentLayer2);

      _navigationLayerConfig.DependentLayers.Returns(new List<string> { "dependent1", "dependent2" });

      _navigationLayer.Initialize(navigation);

      _navigationLayer.Suspend();

      dependentLayer1.Received(1).Suspend(Arg.Any<object>());
      dependentLayer2.Received(1).Suspend(Arg.Any<object>());
    }

    [Test]
    public void Resume_ResumesDependentLayers()
    {
      var dependentLayer1 = Substitute.For<INavigationLayer>();
      var dependentLayer2 = Substitute.For<INavigationLayer>();

      var navigation = Substitute.For<INavigation>();
      navigation["dependent1"].Returns(dependentLayer1);
      navigation["dependent2"].Returns(dependentLayer2);

      _navigationLayerConfig.DependentLayers.Returns(new List<string> { "dependent1", "dependent2" });

      _navigationLayer.Initialize(navigation);
      _navigationLayer.Suspend();

      _navigationLayer.Resume();

      dependentLayer1.Received(1).Resume(Arg.Any<object>());
      dependentLayer2.Received(1).Resume(Arg.Any<object>());
    }

    [Test]
    public async Task OpeningSuspensiveRoute_SuspendsDependentLayers()
    {
      var dependentLayer = Substitute.For<INavigationLayer>();

      var navigation = Substitute.For<INavigation>();
      navigation["dependent1"].Returns(dependentLayer);

      _navigationLayerConfig.DependentLayers.Returns(new List<string> { "dependent1", });

      _navigationLayer.Initialize(navigation);

      await _navigationLayer.Open<SuspensiveUnityRoute>();

      dependentLayer.Received(1).Suspend(Arg.Any<object>());
    }

    [Test]
    public async Task ClosingSuspensiveRoute_ResumesDependentLayers()
    {
      var dependentLayer = Substitute.For<INavigationLayer>();

      var navigation = Substitute.For<INavigation>();
      navigation["dependent1"].Returns(dependentLayer);

      _navigationLayerConfig.DependentLayers.Returns(new List<string> { "dependent1" });

      _navigationLayer.Initialize(navigation);

      await _navigationLayer.Open<SuspensiveUnityRoute>();

      await _navigationLayer.Close<SuspensiveUnityRoute>();

      dependentLayer.Received(1).Resume(Arg.Any<object>());
    }

    [Test]
    public async Task OpeningNonSuspensiveRoute_DoesNotSuspendDependentLayers()
    {
      var dependentLayer = Substitute.For<INavigationLayer>();

      var navigation = Substitute.For<INavigation>();
      navigation["dependent1"].Returns(dependentLayer);

      _navigationLayerConfig.DependentLayers.Returns(new List<string> { "dependent1", });

      _navigationLayer.Initialize(navigation);

      await _navigationLayer.Open<NonSuspensiveUnityRoute>();

      dependentLayer.DidNotReceive().Suspend();
    }

    [Test]
    public async Task OpeningTwoSuspensiveRoutesAndClosingThem_ResumesDependentLayerAfterAllClosed()
    {
      var dependentLayer = Substitute.For<INavigationLayer>();

      var navigation = Substitute.For<INavigation>();
      navigation["dependent1"].Returns(dependentLayer);

      _navigationLayerConfig.DependentLayers.Returns(new List<string> { "dependent1", });

      _navigationLayer.Initialize(navigation);

      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "route1");
      await _navigationLayer.Open<SuspensiveUnityRoute>(id: "route2");
      await _navigationLayer.Close<SuspensiveUnityRoute>(id: "route2");

      dependentLayer.DidNotReceive().Resume();

      await _navigationLayer.Close<SuspensiveUnityRoute>(id: "route1");

      dependentLayer.Received(1).Resume(Arg.Any<object>());
    }

    [Test]
    public async Task MultipleSuspensionsWithDifferentActors_WontResumeUntilBothActorsResume_StaysBlocked()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      var actor1 = new object();
      var actor2 = new object();

      _navigationLayer.Suspend(actor1);
      _navigationLayer.Suspend(actor2);

      _navigationLayer.Resume(actor1);

      Assert.That(_navigationLayer.Stack.All(x => x.IsSuspended), Is.True);
      Assert.That(_navigationLayerConfig.InputBlocker?.IsActive, Is.True);
    }

    [Test]
    public async Task MultipleSuspensionsWithDifferentActors_ResumesAfterBothActorsResume_GetsUnblocked()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      var actor1 = new object();
      var actor2 = new object();

      _navigationLayer.Suspend(actor1);
      _navigationLayer.Suspend(actor2);

      _navigationLayer.Resume(actor1);
      _navigationLayer.Resume(actor2);

      Assert.That(_navigationLayer.Stack.All(x => x.State == RouteState.Open), Is.True);
      Assert.That(_navigationLayer.Stack.All(x => x.IsSuspended), Is.False);
      Assert.That(_navigationLayerConfig.InputBlocker?.IsActive, Is.False);
    }

    [Test]
    public async Task MultipleSuspensionsWithSameActor_ResumesAfterSingleResumeWithSameActor()
    {
      await _navigationLayer.Open<NonSuspensiveUnityRoute>();
      await _navigationLayer.Open<NonSuspensiveUnityRoute>(id: "1");

      var actor = new object();

      _navigationLayer.Suspend(actor);
      _navigationLayer.Suspend(actor);

      _navigationLayer.Resume(actor);

      Assert.That(_navigationLayer.Stack.All(x => x.State == RouteState.Open), Is.True);
      Assert.That(_navigationLayer.Stack.All(x => x.IsSuspended), Is.False);
      Assert.That(_navigationLayerConfig.InputBlocker?.IsActive, Is.False);
    }

    [Test]
    public async Task OpeningTwoRoutesSimultaneously_KeepsInputBlockedUntilBothComplete()
    {
      var slowParameters1 = new SlowViewParameters();
      var slowParameters2 = new SlowViewParameters();

      var task1 = _navigationLayer.Open<SlowRoute, SlowViewParameters>(id: "route1", parameters: slowParameters1);
      var task2 = _navigationLayer.Open<SlowRoute, SlowViewParameters>(id: "route2", parameters: slowParameters2);

      slowParameters1.ShowTcs.TrySetResult();
      await task1;

      var stillBlockedAfterFirstComplete = _navigationLayerConfig.InputBlocker?.IsActive == true;

      slowParameters2.ShowTcs.TrySetResult();
      await task2;

      var unblockedAfterBothComplete = _navigationLayerConfig.InputBlocker?.IsActive == false;

      Assert.That(stillBlockedAfterFirstComplete, Is.True);
      Assert.That(unblockedAfterBothComplete, Is.True);
    }
  }
}
