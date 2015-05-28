#region

using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Security.AccessControl;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace EloFactory_Ahri
{
    internal class Program
    {
        public const string ChampionName = "Ahri";

        public static Orbwalking.Orbwalker Orbwalker;

        public static List<Spell> SpellList = new List<Spell>();

        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Ignite = new Spell(SpellSlot.Unknown, 600);

        public static float QMANA;
        public static float WMANA;
        public static float EMANA;
        public static float RMANA;

        public static Items.Item HealthPotion = new Items.Item(2003, 0);
        public static Items.Item ManaPotion = new Items.Item(2004, 0);
        public static Items.Item CrystallineFlask = new Items.Item(2041, 0);
        public static Items.Item BiscuitofRejuvenation = new Items.Item(2010, 0);

        public static Menu Config;

        private static Obj_AI_Hero Player;

        public static int[] abilitySequence;
        public static int qOff = 0, wOff = 0, eOff = 0, rOff = 0;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            Player = ObjectManager.Player;

            if (Player.ChampionName != ChampionName) return;

            Q = new Spell(SpellSlot.Q, 965f);
            W = new Spell(SpellSlot.W, 650f);
            E = new Spell(SpellSlot.E, 965f);
            R = new Spell(SpellSlot.R, 450f);

            Q.SetSkillshot(0.2f, 100f, 1000f, false, SkillshotType.SkillshotLine);
            W.SetSkillshot(0.7f, 650f, float.MaxValue, false, SkillshotType.SkillshotLine);
            E.SetSkillshot(0.2f, 70f, 1000f, true, SkillshotType.SkillshotLine);

            var ignite = Player.Spellbook.Spells.FirstOrDefault(spell => spell.Name == "summonerdot");
            if (ignite != null)
                Ignite.Slot = ignite.Slot;

            SpellList.Add(Q);
            SpellList.Add(W);
            SpellList.Add(E);
            SpellList.Add(R);

            abilitySequence = new int[] { 1, 3, 1, 2, 1, 4, 1, 2, 1, 3, 4, 2, 2, 2, 3, 4, 3, 3 };

            Config = new Menu(ChampionName + " By LuNi", ChampionName + " By LuNi", true);

            Config.AddSubMenu(new Menu("Orbwalking", "Orbwalking"));

            var targetSelectorMenu = new Menu("Target Selector", "Target Selector");
            TargetSelector.AddToMenu(targetSelectorMenu);
            Config.AddSubMenu(targetSelectorMenu);

            Orbwalker = new Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"));

            Config.AddSubMenu(new Menu("Combo", "Combo"));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ahri.UseQCombo", "Use Q In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ahri.UseWCombo", "Use W In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ahri.UseECombo", "Use E In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ahri.UseRCombo", "Use R + E If Killable In Combo").SetValue(true));
            Config.SubMenu("Combo").AddItem(new MenuItem("Ahri.UseIgnite", "Use Ignite").SetValue(true));

            Config.AddSubMenu(new Menu("Harass", "Harass"));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.UseQHarass", "Use Q In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.QMiniManaHarass", "Minimum Mana To Use Q In Harass").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.UseQOnlyEHarass", "Use Q Only When E Hit In Harass ").SetValue(false));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.UseWHarass", "Use W In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.WMiniManaHarass", "Minimum Mana To Use W In Harass").SetValue(new Slider(50, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.UseWOnlyEHarass", "Use W Only When E Hit In Harass ").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.UseEHarass", "Use E In Harass").SetValue(true));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.EMiniManaHarass", "Minimum Mana To Use E In Harass").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.HarassActive", "Harass!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
            Config.SubMenu("Harass").AddItem(new MenuItem("Ahri.HarassActiveT", "Harass (toggle)!").SetValue(new KeyBind("Y".ToCharArray()[0], KeyBindType.Toggle)));

            Config.AddSubMenu(new Menu("LastHit", "LastHit"));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ahri.UseQLastHit", "Use Q In LastHit").SetValue(false));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ahri.QMiniManaLastHit", "Minimum Mana To Use Q In LastHit").SetValue(new Slider(80, 0, 100)));
            Config.SubMenu("LastHit").AddItem(new MenuItem("Ahri.SafeQLastHit", "Never Use Q In LastHit When Enemy Close To Your Position").SetValue(false));

            Config.AddSubMenu(new Menu("LaneClear", "LaneClear"));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ahri.UseQLaneClear", "Use Q in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ahri.QMiniManaLaneClear", "Minimum Mana To Use Q In LaneClear").SetValue(new Slider(30, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ahri.QLaneClearCount", "Minimum Minion To Use Q In LaneClear").SetValue(new Slider(3, 1, 6)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ahri.UseWLaneClear", "Use W in LaneClear").SetValue(true));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ahri.WMiniManaLaneClear", "Minimum Mana To Use W In LaneClear").SetValue(new Slider(70, 0, 100)));
            Config.SubMenu("LaneClear").AddItem(new MenuItem("Ahri.WLaneClearCount", "Minimum Minion To Use W In LaneClear").SetValue(new Slider(4, 1, 6)));

            Config.AddSubMenu(new Menu("JungleClear", "JungleClear"));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.UseQJungleClear", "Use Q In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.QMiniManaJungleClear", "Minimum Mana To Use Q In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.UseWJungleClear", "Use W In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.WMiniManaJungleClear", "Minimum Mana To Use W In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.UseEJungleClear", "Use E In JungleClear").SetValue(true));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.EMiniManaJungleClear", "Minimum Mana To Use E In JungleClear").SetValue(new Slider(0, 0, 100)));
            Config.SubMenu("JungleClear").AddItem(new MenuItem("Ahri.SafeJungleClear", "Dont Use Spell In Jungle Clear If Enemy in Dangerous Range").SetValue(true));

            Config.AddSubMenu(new Menu("Misc", "Misc"));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.EInterrupt", "Interrupt Spells With E").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoEEGC", "Auto E On Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoWEGC", "Auto W On Gapclosers").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoEWhenEnemyCast", "Always Auto Use E On Enemy Attack").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoQWhenEnemyCast", "Always Auto Use Q On Enemy Attack").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoQWhenE", "Only Auto Use Q When E Hit").SetValue(false));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoPotion", "Use Auto Potion").SetValue(true));
            Config.SubMenu("Misc").AddItem(new MenuItem("Ahri.AutoLevelSpell", "Auto Level Spell").SetValue(true));

            Config.AddSubMenu(new Menu("Drawings", "Drawings"));
            Config.SubMenu("Drawings").AddItem(new MenuItem("QRange", "Q range").SetValue(new Circle(true, Color.Indigo)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("WRange", "W range").SetValue(new Circle(true, Color.Green)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("ERange", "E range").SetValue(new Circle(true, Color.Green)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("RRange", "R range").SetValue(new Circle(true, Color.Gold)));
            Config.SubMenu("Drawings").AddItem(new MenuItem("DrawOrbwalkTarget", "Draw Orbwalk target").SetValue(true));

            Config.AddToMainMenu();

            Game.OnUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;


        }

        #region ToogleOrder Game_OnUpdate
        public static void Game_OnGameUpdate(EventArgs args)
        {

            if (Config.Item("Ahri.AutoLevelSpell").GetValue<bool>()) LevelUpSpells();

            if (Player.IsDead) return;

            if (Player.GetBuffCount("Recall") == 1) return;

            ManaManager();
            PotionManager();

            KillSteal();

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
            {
                Combo();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
            {
                LaneClear();
                JungleClear();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
            {
                LastHit();
            }

            if (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
            {
                LastHit();
            }

            if (Config.Item("Ahri.HarassActive").GetValue<KeyBind>().Active || Config.Item("Ahri.HarassActiveT").GetValue<KeyBind>().Active)
            {
                Harass();
            }

        }
        #endregion

        #region Interupt OnProcessSpellCast
        public static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs args)
        {
            double ShouldUseOn = ShouldUse(args.SData.Name);
            if (unit.Team != ObjectManager.Player.Team && ShouldUseOn >= 0f && unit.IsValidTarget(Q.Range))
            {

                if (Config.Item("Ahri.EInterrupt").GetValue<bool>() && E.IsReady() && Player.Mana >= EMANA && Player.Distance(unit) < E.Range - 25)
                {
                    E.CastIfHitchanceEquals(unit, HitChance.High, true);
                }

            }

            if (Config.Item("Ahri.AutoEWhenEnemyCast").GetValue<bool>() && (unit.IsValid<Obj_AI_Hero>() && !unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe && E.IsReady() && Player.Distance(unit) < E.Range - 25)
            {
                E.CastIfHitchanceEquals(unit, HitChance.High, true);
            }

            if (((Config.Item("Ahri.AutoQWhenEnemyCast").GetValue<bool>() && (unit.IsValid<Obj_AI_Hero>() && !unit.IsValid<Obj_AI_Turret>()) && unit.IsEnemy && args.Target.IsMe) || (Config.Item("Ahri.AutoQWhenE").GetValue<bool>() && unit.HasBuff("AhriSeduce"))) && Q.IsReady() && Player.Distance(unit) < Q.Range - 25)
            {
                Q.CastIfHitchanceEquals(unit, HitChance.High, true);
            }
        }
        #endregion

        #region AntiGapCloser
        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

            if (Config.Item("Ahri.AutoEEGC").GetValue<bool>() && E.IsReady() && (Player.Mana >= EMANA + RMANA || Player.Mana < RMANA * 0.8) && Player.Distance(gapcloser.Sender) < E.Range - 20)
            {
                E.CastIfHitchanceEquals(gapcloser.Sender, HitChance.High, true);
            }

            if (Config.Item("Ahri.AutoWEGC").GetValue<bool>() && W.IsReady() && (Player.Mana >= WMANA + RMANA || Player.Mana < RMANA * 0.8) && Player.Distance(gapcloser.Sender) < W.Range - 20)
            {
                W.Cast();
            }

        }
        #endregion

        #region Combo
        public static void Combo()
        {

            var useQ = Program.Config.Item("Ahri.UseQCombo").GetValue<bool>();
            var useW = Program.Config.Item("Ahri.UseWCombo").GetValue<bool>();
            var useE = Program.Config.Item("Ahri.UseECombo").GetValue<bool>();
            var useR = Program.Config.Item("Ahri.UseRCombo").GetValue<bool>();

            var target = TargetSelector.GetTarget(Q.Range + R.Range, TargetSelector.DamageType.Magical);
            if (target.IsValidTarget())
            {
                #region Sort E combo mode
                if (useE && E.IsReady() && Player.Mana >= EMANA && Player.Distance(target) < E.Range)
                {
                    E.CastIfHitchanceEquals(target, HitChance.High, true);
                }
                #endregion

                #region Sort R combo mode
                if (useR && R.IsReady() && E.IsReady() && Player.Mana > RMANA + QMANA + WMANA + EMANA && Player.HealthPercent >= target.HealthPercent)
                {
                    if (Player.Distance(target) < R.Range + E.Range - 75 && Player.Distance(target) > E.Range - 50 && ComboDamage(target) > target.Health)
                    {
                        R.Cast(target.ServerPosition, true);
                    }
                }
                #endregion

                #region Sort W combo mode
                if (useW && W.IsReady() && Player.Mana >= WMANA && Player.Distance(target) < W.Range)
                {
                    W.Cast();                    
                }
                #endregion

                #region Sort Q combo mode
                if (useQ && Q.IsReady() && Player.Mana >= QMANA && Player.Distance(target) < Q.Range)
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, true);
                }
                #endregion
            }

        }
        #endregion

        #region Harass
        public static void Harass()
        {

            var targetH = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            var useQ = Program.Config.Item("Ahri.UseQHarass").GetValue<bool>();
            var useW = Program.Config.Item("Ahri.UseWHarass").GetValue<bool>();
            var useE = Program.Config.Item("Ahri.UseEHarass").GetValue<bool>();

            var QMinMana = Config.Item("Ahri.QMiniManaHarass").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Ahri.WMiniManaHarass").GetValue<Slider>().Value;
            var EMinMana = Config.Item("Ahri.EMiniManaHarass").GetValue<Slider>().Value;


            if (useE && E.IsReady() && Player.Distance(targetH) < E.Range && Player.ManaPercent >= EMinMana)
            {
                E.CastIfHitchanceEquals(targetH, HitChance.High, true);
            }

            if (useQ && (!useE || !Config.Item("Ahri.UseQOnlyEHarass").GetValue<bool>() || (Config.Item("Ahri.UseQOnlyEHarass").GetValue<bool>() && (targetH.HasBuffOfType(BuffType.Stun) || targetH.HasBuffOfType(BuffType.Snare) || targetH.HasBuffOfType(BuffType.Charm) || targetH.HasBuffOfType(BuffType.Fear) || targetH.HasBuffOfType(BuffType.Taunt)))) && Q.IsReady() && Player.Distance(targetH) < Q.Range && Player.ManaPercent >= QMinMana)
            {
                Q.CastIfHitchanceEquals(targetH, HitChance.High, true);
            }

            if (useW && (!useE || !Config.Item("Ahri.UseWOnlyEHarass").GetValue<bool>() || (Config.Item("Ahri.UseWOnlyEHarass").GetValue<bool>() && (targetH.HasBuffOfType(BuffType.Stun) || targetH.HasBuffOfType(BuffType.Snare) || targetH.HasBuffOfType(BuffType.Charm) || targetH.HasBuffOfType(BuffType.Fear) || targetH.HasBuffOfType(BuffType.Taunt)))) && W.IsReady() && Player.Distance(targetH) < W.Range && Player.ManaPercent >= WMinMana)
            {
                W.Cast();
            }


        }
        #endregion

        #region LastHit
        public static void LastHit()
        {

            var useQ = Program.Config.Item("Ahri.UseQLastHit").GetValue<bool>();

            var QMinMana = Config.Item("Ahri.QMiniManaLastHit").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy);
            var MinionQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Enemy, MinionOrderTypes.MaxHealth).FirstOrDefault();

            foreach (var minion in allMinionsQ)
            {
                if (useQ && Q.IsReady() && minion.Health > Player.GetAutoAttackDamage(minion) && minion.Health < Q.GetDamage(minion) * 0.9)
                {
                    Q.CastIfHitchanceEquals(minion, HitChance.High, true);
                }
            }

        }
        #endregion

        #region LaneClear
        public static void LaneClear()
        {

            var useQ = Program.Config.Item("Ahri.UseQLaneClear").GetValue<bool>();
            var useW = Program.Config.Item("Ahri.UseWLaneClear").GetValue<bool>();

            var QMinMana = Config.Item("Ahri.QMiniManaLaneClear").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Ahri.WMiniManaLaneClear").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All);

            if (useQ && Q.IsReady() && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
            {

                var Qfarm = W.GetLineFarmLocation(allMinionsQ, Q.Width);
                if (Qfarm.MinionsHit >= Config.Item("Ahri.QLaneClearCount").GetValue<Slider>().Value && Q.IsReady())
                    Q.Cast(Qfarm.Position);
            }

            if (useW && W.IsReady() && Player.Mana >= WMANA + QMANA && Player.ManaPercent >= WMinMana)
            {
                if (allMinionsQ.Count(x => x.IsValidTarget(W.Range)) >= Config.Item("Ahri.WLaneClearCount").GetValue<Slider>().Value)
                {
                    W.Cast();
                }
            }

        }
        #endregion

        #region JungleClear
        public static void JungleClear()
        {

            var useQ = Program.Config.Item("Ahri.UseQJungleClear").GetValue<bool>();
            var useW = Program.Config.Item("Ahri.UseWJungleClear").GetValue<bool>();
            var useE = Program.Config.Item("Ahri.UseWJungleClear").GetValue<bool>();

            var QMinMana = Config.Item("Ahri.QMiniManaHarass").GetValue<Slider>().Value;
            var WMinMana = Config.Item("Ahri.WMiniManaHarass").GetValue<Slider>().Value;
            var EMinMana = Config.Item("Ahri.EMiniManaHarass").GetValue<Slider>().Value;

            var allMinionsQ = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral);
            var MinionN = MinionManager.GetMinions(Q.Range, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth).FirstOrDefault();

            if (Config.Item("Ahri.SafeJungleClear").GetValue<bool>() && Player.CountEnemiesInRange(1500) > 0) return;

            if (!MinionN.IsValidTarget() || MinionN == null)
            {
                LaneClear();
                return;
            }

            if (useE && E.IsReady() && Player.Distance(MinionN) < E.Range && Player.Mana >= EMANA + QMANA && Player.ManaPercent >= EMinMana)
            {
                E.CastIfHitchanceEquals(MinionN, HitChance.High, true);
            }

            if (useW && W.IsReady() && Player.Distance(MinionN) < Player.AttackRange && Player.Mana >= WMANA + QMANA && Player.ManaPercent >= WMinMana)
            {
                W.Cast();
            }

            if (useQ && Q.IsReady() && Player.Distance(MinionN) < Q.Range && Player.Mana >= QMANA && Player.ManaPercent >= QMinMana)
            {
                Q.CastIfHitchanceEquals(MinionN, HitChance.High, true);
            }
            


        }
        #endregion

        #region KillSteal
        public static void KillSteal()
        {

            foreach (var target in ObjectManager.Get<Obj_AI_Hero>().Where(target => !target.IsMe && target.Team != ObjectManager.Player.Team))
            {

                if (W.IsReady() && Player.Mana >= WMANA && target.Health < W.GetDamage(target) && Player.Distance(target) < W.Range - 50 && Player.CountEnemiesInRange(W.Range) == 1 && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast();
                    return;
                }

                if (Q.IsReady() && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) && Player.Distance(target) < Q.Range - 50 && !target.IsDead && target.IsValidTarget())
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (E.IsReady() && Player.Mana >= EMANA && target.Health < E.GetDamage(target) && Player.Distance(target) < E.Range - 50 && !target.IsDead && target.IsValidTarget())
                {
                    E.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (Ignite.Slot != SpellSlot.Unknown && Config.Item("Ahri.UseIgnite").GetValue<bool>() && Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) > target.Health && target.IsValidTarget(Ignite.Range))
                {
                    Ignite.Cast(target, true);
                }

                if (W.IsReady() && Q.IsReady() && Player.Mana >= WMANA + QMANA && target.Health < W.GetDamage(target) + Q.GetDamage(target) && Player.Distance(target) < W.Range - 50 && Player.CountEnemiesInRange(W.Range) == 1 && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast();
                    return;
                }

                if (W.IsReady() && E.IsReady() && Player.Mana >= WMANA + EMANA && target.Health < W.GetDamage(target) + E.GetDamage(target) && Player.Distance(target) < E.Range - 50 && Player.CountEnemiesInRange(W.Range) == 1 && !target.IsDead && target.IsValidTarget())
                {
                    E.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (Q.IsReady() && E.IsReady() && Player.Mana >= QMANA + EMANA && target.Health < E.GetDamage(target) + Q.GetDamage(target) && Player.Distance(target) < E.Range - 50 && !target.IsDead && target.IsValidTarget())
                {
                    E.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (Ignite.Slot != SpellSlot.Unknown && W.IsReady() && Config.Item("Ahri.UseIgnite").GetValue<bool>() && Player.Mana >= WMANA && target.Health < W.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < W.Range - 50 && Player.CountEnemiesInRange(W.Range) == 1 && !target.IsDead && target.IsValidTarget())
                {
                    W.Cast();
                    return;
                }

                if (Ignite.Slot != SpellSlot.Unknown && Q.IsReady() && Config.Item("Ahri.UseIgnite").GetValue<bool>() && Player.Mana >= QMANA && target.Health < Q.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < 600 && !target.IsDead && target.IsValidTarget())
                {
                    Q.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

                if (Ignite.Slot != SpellSlot.Unknown && E.IsReady() && Config.Item("Ahri.UseIgnite").GetValue<bool>() && Player.Mana >= EMANA && target.Health < E.GetDamage(target) + Player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite) && Player.Distance(target) < E.Range - 50 && !target.IsDead && target.IsValidTarget())
                {
                    E.CastIfHitchanceEquals(target, HitChance.High, true);
                    return;
                }

            }
        }
        #endregion

        #region PlayerDamage
        public static float ComboDamage(Obj_AI_Hero hero)
        {
            double damage = 0;
            if (E.IsReady())
            {
                damage += Damage.GetSpellDamage(Player, hero, SpellSlot.E);
            }
            if (Q.IsReady())
            {
                damage += Damage.GetSpellDamage(Player, hero, SpellSlot.Q);
            }
            if (W.IsReady())
            {
                damage += (float)Damage.GetSpellDamage(Player, hero, SpellSlot.W);
            }
            if (Player.Spellbook.CanUseSpell(Player.GetSpellSlot("summonerdot")) == SpellState.Ready)
            {
                damage += (float)Player.GetSummonerSpellDamage(hero, Damage.SummonerSpell.Ignite);
            }
            return (float)damage;
        }
        #endregion

        #region Interupt Spell List
        public static double ShouldUse(string SpellName)
        {
            if (SpellName == "KatarinaR")
                return 0;
            if (SpellName == "AlZaharNetherGrasp")
                return 0;
            if (SpellName == "GalioIdolOfDurand")
                return 0;
            if (SpellName == "LuxMaliceCannon")
                return 0;
            if (SpellName == "MissFortuneBulletTime")
                return 0;
            if (SpellName == "CaitlynPiltoverPeacemaker")
                return 0;
            if (SpellName == "EzrealTrueshotBarrage")
                return 0;
            if (SpellName == "InfiniteDuress")
                return 0;
            if (SpellName == "VelkozR")
                return 0;
            if (SpellName == "XerathLocusOfPower2")
                return 0;
            if (SpellName == "Drain")
                return 0;
            if (SpellName == "Crowstorm")
                return 0;
            if (SpellName == "ReapTheWhirlwind")
                return 0;
            if (SpellName == "FallenOne")
                return 0;
            if (SpellName == "JudicatorIntervention")
                return 0;
            if (SpellName == "KennenShurikenStorm")
                return 0;
            if (SpellName == "LucianR")
                return 0;
            if (SpellName == "SoulShackles")
                return 0;
            if (SpellName == "NamiQ")
                return 0;
            if (SpellName == "AbsoluteZero")
                return 0;
            if (SpellName == "Pantheon_GrandSkyfall_Jump")
                return 0;
            if (SpellName == "RivenMartyr")
                return 0;
            if (SpellName == "RivenTriCleave_03")
                return 0;
            if (SpellName == "RunePrison")
                return 0;
            if (SpellName == "SkarnerImpale")
                return 0;
            if (SpellName == "UndyingRage")
                return 0;
            if (SpellName == "VarusQ")
                return 0;
            if (SpellName == "MonkeyKingSpinToWin")
                return 0;
            if (SpellName == "YasuoRKnockUpComboW")
                return 0;
            if (SpellName == "ZacE")
                return 0;
            if (SpellName == "ZacR")
                return 0;
            if (SpellName == "UrgotSwap2")
                return 0;
            return -1;
        }
        # endregion

        #region ManaManager
        public static void ManaManager()
        {

            QMANA = Q.Instance.ManaCost;
            WMANA = W.Instance.ManaCost;
            EMANA = E.Instance.ManaCost;
            RMANA = R.Instance.ManaCost;

            if (ObjectManager.Player.Health < ObjectManager.Player.MaxHealth * 0.2)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
            }
        }
        #endregion

        #region PotionManager
        public static void PotionManager()
        {
            if (Player.Level == 1 && Player.CountEnemiesInRange(1000) == 1 && Player.Health >= Player.MaxHealth * 0.35) return;
            if (Player.Level == 1 && Player.CountEnemiesInRange(1000) == 2 && Player.Health >= Player.MaxHealth * 0.50) return;

            if (Config.Item("Ahri.AutoPotion").GetValue<bool>() && !Player.InFountain() && !Player.IsRecalling() && !Player.IsDead)
            {
                #region BiscuitofRejuvenation
                if (BiscuitofRejuvenation.IsReady() && !Player.HasBuff("ItemMiniRegenPotion") && !Player.HasBuff("ItemCrystalFlask"))
                {

                    if (Player.MaxHealth > Player.Health + 170 && Player.MaxMana > Player.Mana + 10 && Player.CountEnemiesInRange(1000) > 0 &&
                        Player.Health < Player.MaxHealth * 0.75)
                    {
                        BiscuitofRejuvenation.Cast();
                    }

                    else if (Player.MaxHealth > Player.Health + 170 && Player.MaxMana > Player.Mana + 10 && Player.CountEnemiesInRange(1000) == 0 &&
                        Player.Health < Player.MaxHealth * 0.6)
                    {
                        BiscuitofRejuvenation.Cast();
                    }

                }
                #endregion

                #region HealthPotion
                else if (HealthPotion.IsReady() && !Player.HasBuff("RegenerationPotion") && !Player.HasBuff("ItemCrystalFlask"))
                {

                    if (Player.MaxHealth > Player.Health + 150 && Player.CountEnemiesInRange(1000) > 0 &&
                        Player.Health < Player.MaxHealth * 0.75)
                    {
                        HealthPotion.Cast();
                    }

                    else if (Player.MaxHealth > Player.Health + 150 && Player.CountEnemiesInRange(1000) == 0 &&
                        Player.Health < Player.MaxHealth * 0.6)
                    {
                        HealthPotion.Cast();
                    }

                }
                #endregion

                #region CrystallineFlask
                else if (CrystallineFlask.IsReady() && !Player.HasBuff("ItemCrystalFlask") && !Player.HasBuff("RegenerationPotion") && !Player.HasBuff("FlaskOfCrystalWater") && !Player.HasBuff("ItemMiniRegenPotion"))
                {

                    if (Player.MaxHealth > Player.Health + 120 && Player.MaxMana > Player.Mana + 60 && Player.CountEnemiesInRange(1000) > 0 &&
                        (Player.Health < Player.MaxHealth * 0.85 || Player.Mana < Player.MaxMana * 0.65))
                    {
                        CrystallineFlask.Cast();
                    }

                    else if (Player.MaxHealth > Player.Health + 120 && Player.MaxMana > Player.Mana + 60 && Player.CountEnemiesInRange(1000) == 0 &&
                        (Player.Health < Player.MaxHealth * 0.7 || Player.Mana < Player.MaxMana * 0.5))
                    {
                        CrystallineFlask.Cast();
                    }

                }
                #endregion

                #region ManaPotion
                else if (ManaPotion.IsReady() && !Player.HasBuff("FlaskOfCrystalWater") && !Player.HasBuff("ItemCrystalFlask"))
                {

                    if (Player.MaxMana > Player.Mana + 100 && Player.CountEnemiesInRange(1000) > 0 &&
                        Player.Mana < Player.MaxMana * 0.7)
                    {
                        ManaPotion.Cast();
                    }

                    else if (Player.MaxMana > Player.Mana + 100 && Player.CountEnemiesInRange(1000) == 0 &&
                        Player.Mana < Player.MaxMana * 0.4)
                    {
                        ManaPotion.Cast();
                    }

                }
                #endregion
            }
        }
        #endregion

        #region DrawingRange
        public static void Drawing_OnDraw(EventArgs args)
        {

            foreach (var spell in SpellList)
            {
                var menuItem = Config.Item(spell.Slot + "Range").GetValue<Circle>();
                if (menuItem.Active && (spell.Slot != SpellSlot.R || R.Level > 0))
                    Render.Circle.DrawCircle(Player.Position, spell.Range, menuItem.Color);
            }

            if (Config.Item("DrawOrbwalkTarget").GetValue<bool>())
            {
                var orbT = Orbwalker.GetTarget();
                if (orbT.IsValidTarget())
                    Render.Circle.DrawCircle(orbT.Position, 100, System.Drawing.Color.Pink);
            }

        }
        #endregion

        #region Up Spell
        private static void LevelUpSpells()
        {
            int qL = Player.Spellbook.GetSpell(SpellSlot.Q).Level + qOff;
            int wL = Player.Spellbook.GetSpell(SpellSlot.W).Level + wOff;
            int eL = Player.Spellbook.GetSpell(SpellSlot.E).Level + eOff;
            int rL = Player.Spellbook.GetSpell(SpellSlot.R).Level + rOff;
            if (qL + wL + eL + rL < ObjectManager.Player.Level)
            {
                int[] level = new int[] { 0, 0, 0, 0 };
                for (int i = 0; i < ObjectManager.Player.Level; i++)
                {
                    level[abilitySequence[i] - 1] = level[abilitySequence[i] - 1] + 1;
                }
                if (qL < level[0]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.Q);
                if (wL < level[1]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.W);
                if (eL < level[2]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.E);
                if (rL < level[3]) ObjectManager.Player.Spellbook.LevelSpell(SpellSlot.R);

            }
        }
        #endregion

    }

}
