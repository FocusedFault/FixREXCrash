using BepInEx;
using RoR2;
using System;
using MonoMod.Cil;

namespace FixREXCrash
{
  [BepInPlugin("com.Nuxlar.FixREXCrash", "FixREXCrash", "1.0.2")]

  public class FixREXCrash : BaseUnityPlugin
  {

    public void Awake()
    {
      IL.EntityStates.Treebot.Weapon.FireSonicBoom.OnEnter += ILFix;
    }

    private void ILFix(ILContext il)
    {
      ILCursor c = new ILCursor(il);
      c.GotoNext(MoveType.After,
        x => x.MatchLdloc(out _),
        x => x.MatchCallOrCallvirt<CharacterBody>("get_acceleration"),
        x => x.MatchStloc(out _)
      );
      c.Index--;
      c.EmitDelegate<Func<float, float>>((cb) =>
      {
        if (double.IsNaN(cb))
          return 0f;
        else
          return cb;
      });
    }
  }
}
