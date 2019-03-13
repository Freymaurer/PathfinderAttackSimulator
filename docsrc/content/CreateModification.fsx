(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I "../../src/PathfinderAttackSimulator/bin/Release/netstandard2.0"

(**
Creating a Modification
======================

Example on how a Modifcation should be created.
-------

A modification needs certain informations for a correct calculation. 
Right now this is not hand-held at all, but might get better in a future version.

This is by far the most complex variant of information-type.
Therefore, i create a library of pre-written modifications.

Let's see how this works for the famous Haste spell

### Haste
*)

#r "PathfinderAttackSimulator.dll"
open PathfinderAttackSimulator.Library.AuxLibFunctions

let Haste = {
    Name = "Haste"
    BonusAttacks = createBonusAttacks 1 HasteLike PrimaryMain
    BonusAttackRoll = createBonus 1 Flat
    BonusDamage = createBonus 0 Flat
    ExtraDamage = createDamage 0 0 Untyped
    AppliedTo = [|All|], -20
    StatChanges = [||]
    SizeChanges = createSizechange 0 Flat false
    Description = "Range: Close (25 ft. + 5 ft./2 levels). One creature/level, no two of which can be more than 30 ft. apart."
    }

(**
So a short introduction to the spell for all who might not know it (so like complete beginners, i guess?).
> Haste gives an extra attack, that does not stack with weapon enhancements like Speed and so on, and also an 
> flat bonus to all attack rolls, and also some non - attack related boni.

So how does this work:

* Name = the actual correct name of the weapon, currently not relevant for the code
* BonusAttacks = the number of bonus attacks granted by the modification and which WeaponType should be used for the extra attack

> Imagine a dual-wielding fighter with a silver,PrimaryMain and an cold iron sword,Primary fighting against a fae creature.

* BonusDamage = bonus damage that is added to the weapon damage and the bonus type (think: Prayer or InspireCourage)
* ExtraDamage = Damage that will be calculated and displayed separately, e.g. Sneak Attack from rogue.
* AppliedTo = apply this modification to the WeaponTypes specified here and how often. "-20" is currently used as a placeholder for "to all attacks". Here you could use 1 for Spellstrike-ShockingGrasp.
* StatChanges = e.g. the alchemist's mutagen (createStatChange "Str" 2 Alchemical) the number again, represents the modifier increase. Can be left empty if no stat change applies.
* SizeChanges = The first value represents number of size changes and the direction, e.g. -1 = shrink by 1 size category. Next Value will be mostly "Polymorph" or Flat as the type of size change.
Last is a false/true question, whether this size change is an actual size change or an effective size change: Write "true" if it is just an effective size change (Improved Natural Armor).
* Description = A short Description for additional information. Not used in the script right now.

Some further Examples:

### Alchemist's Strength Mutagen

This is a nice example to showcase the StatChanges attribute.
*)
let MutagenStrength = {
    Name = "Strength Mutagen"
    BonusAttacks = createBonusAttacks 0 NoBA All
    BonusAttackRoll = createBonus 0 Flat
    BonusDamage = createBonus 0 Flat
    ExtraDamage = createDamage 0 0 Untyped
    AppliedTo = [|All|], -20
    StatChanges = [|(createStatChange Strength 2 Alchemical); (createStatChange Intelligence -2 Alchemical)|]
    SizeChanges = createSizechange 0 Flat false
    Description = ""
    }

(**
### Enlarge Person

Pay attention to the StatChanges and the SizeChanges. There is also no -1 at "BonusAttackRoll", because size-boni/mali to hit an attack are calculated automatically.
*)
let EnlargePerson = {
    Name = "Enlarge Person"
    BonusAttacks = createBonusAttacks 0 NoBA All
    BonusAttackRoll = createBonus 0 Flat
    BonusDamage = createBonus 0 Flat
    ExtraDamage = createDamage 0 0 Untyped
    AppliedTo = [|All|], -20
    StatChanges = [|(createStatChange Strength 1 Size);(createStatChange Dexterity -1 Size)|]
    SizeChanges = createSizechange 1 Polymorph false
    Description = ""
    }

(**
### Sneak Attack (from invisibility)

Here you can see that you can even make these modifications to small automatic functions. This one calculates the SneakAttack damage for a full rogue progression.
To do this it needs additional information in form of the rogue level.
Also this bonus is meant to be applied only once, therefore we have under "AppliedTo" [|All|], 1; because this will we applied to any weapon that is calculated first and then only to the first attack.
*)
let SneakAttackOnce rogueLevel = {
    Name = "Sneak Attack on first attack"
    BonusAttacks = createBonusAttacks 0 NoBA All
    BonusAttackRoll = createBonus 0 Flat
    BonusDamage = createBonus 0 Flat
    ExtraDamage = createDamage (int (ceil (float rogueLevel/2.))) 6 Precision
    AppliedTo = [|All|], 1        
    StatChanges = [||]
    SizeChanges = createSizechange 0 Flat false
    Description = "Sneak Attack on first attack. This can happen due to a stealth attack or an full-round attack action from invisibility; use this only if you have full rogue sneak attack damage dice progression"
    }
(**
This type of automatic calculation is also already done for things like PowerAttack or FuriousFocus. But it can also be useful for Spells to automatically calculate any given boni from the caster level.
*)