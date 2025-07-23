using UnityEngine;
using UnityEngine.UI;

namespace Viewigation.Blocks
{
  [RequireComponent(typeof(GraphicRaycaster))]
  public class CanvasInputBlocker : BaseInputBlocker
  {
    private GraphicRaycaster _graphicRaycaster = null!;

    public void Awake()
    {
      _graphicRaycaster = GetComponent<GraphicRaycaster>();
    }

    protected override void SetBlocked(bool value)
    {
      _graphicRaycaster.enabled = !value;
    }
  }
}
