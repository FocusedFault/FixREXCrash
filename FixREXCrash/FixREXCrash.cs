using BepInEx;
using MonoMod.Cil;
using RoR2;

namespace FixREXCrash
{
  [BepInPlugin("com.Nuxlar.FixREXCrash", "FixREXCrash", "1.0.0")]

  public class FixREXCrash : BaseUnityPlugin
  {

    public void Awake()
    {
      IL.EntityStates.Treebot.Weapon.FireSonicBoom.OnEnter += FireSonicBoom_OnEnter;
    }

    private void FireSonicBoom_OnEnter(ILContext context)
    {
      ILCursor cursor = new(context);
      cursor.GotoNext(MoveType.After, instruction => instruction.MatchCallOrCallvirt(
          typeof(CharacterBody).GetProperty(nameof(CharacterBody.acceleration)).GetMethod));
      cursor.EmitDelegate((float acceleration) => float.IsNaN(acceleration) ? 1 : acceleration);
    }

  }
}
