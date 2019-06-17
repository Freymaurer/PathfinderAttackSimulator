﻿namespace PathfinderAttackSimulator

open System

open PathfinderAttackSimulator
open Library.AuxLibFunctions


/// Library for all pre-written modifications
module CoreFunctions =

    let findSizes = [1,createSizeAttributes 8 1 Fine;
                        2,createSizeAttributes 4 2 Diminuitive;
                        3,createSizeAttributes 2 3 Tiny;
                        4,createSizeAttributes 1 4 Small;
                        5,createSizeAttributes 0 5 Medium;
                        6,createSizeAttributes -1 6 Large;
                        7,createSizeAttributes -2 7 Huge;
                        8,createSizeAttributes -4 8 Gargantuan;
                        9,createSizeAttributes -8 9 Colossal
                        ] |> Map.ofSeq

    /// calculates real size changes due to modifications and applies them to the start size.
    /// This function returns an integer representing the new size (The map of size integer to size is "findSizes"
    let calculateSize (size: SizeType) (modifications: AttackModification [])=

        let startSize =
            match size with
            | Fine          -> 1
            | Diminuitive   -> 2
            | Tiny          -> 3
            | Small         -> 4
            | Medium        -> 5
            | Large         -> 6
            | Huge          -> 7
            | Gargantuan    -> 8
            | Colossal      -> 9

        let changeSizeBy =
            modifications
            |> Array.filter (fun x -> x.SizeChanges.EffectiveSizeChange = false)
            |> Array.map (fun x -> x.SizeChanges)
            |> Array.groupBy (fun x -> x.SizeChangeBonustype)
            |> Array.map (fun (header,bonusArr) -> if header <> BonusTypes.Flat 
                                                   then bonusArr
                                                        |> Array.sortByDescending (fun x -> x.SizeChangeValue) 
                                                        |> fun x -> Array.head x
                                                        |> fun x -> x.SizeChangeValue
                                                   elif header = BonusTypes.Flat
                                                   then bonusArr
                                                        |> Array.map (fun x -> x.SizeChangeValue)
                                                        |> Array.sum
                                                   else failwith "Unrecognized Pattern of size changes in 'calculateSize'" 
                         )
            |> Array.sum

        (startSize + changeSizeBy)
        |> fun x -> if x > 9 then 9
                    elif x < 1 then 1
                    else x

    module OneAttack =

        module toHit =

            /// calculates size bonus to attack rolls (eg. +1 for small)
            let addSizeBonus (size: SizeType) (modifications: AttackModification [])=
                calculateSize size modifications
                |> fun x -> Map.find x findSizes
                |> fun x -> x.SizeModifier

            
            /// calculates bonus on attack rolls due to the ability score used by the weapon. 
            /// This function includes changes to these ability score modifiers due to modifications.
            let getUsedModifierToHit (char: CharacterStats) (weapon: Weapon) (modifications: AttackModification []) =
            
                let getStatChangesToHit =
                    modifications
                    |> Array.collect (fun x -> x.StatChanges)
                    |> Array.filter (fun statChange -> statChange.Attribute = weapon.Modifier.ToHit)
                    |> Array.groupBy (fun statChange -> statChange.Bonustype)
                    |> Array.map (fun (uselessHeader,x) -> x)
                    ///Next step should take the highest stat change to remove non-stacking boni
                    ///But what if a negative and a positive bonus of the same type exist?
                    |> Array.map (fun x -> Array.sortByDescending (fun statChange -> statChange.AttributeChange) x)
                    |> Array.map (fun x -> Array.head x)
                    |> Array.map (fun statChange -> statChange.AttributeChange)
                    |> Array.sum
            
                (match weapon.Modifier.ToHit with
                | Strength      -> char.Strength
                | Dexterity     -> char.Dexterity
                | Constitution  -> char.Constitution
                | Intelligence  -> char.Intelligence
                | Wisdom        -> char.Wisdom
                | Charisma      -> char.Charisma
                | _             -> 10
                )
                |> fun x -> x + getStatChangesToHit
                |> fun x -> (float x-10.)/2.
                |> floor |> int
            
            /// calculates all boni to attack rolls from modifications and checks if they stack or not
            let addBoniToAttack (modifications: AttackModification []) = 
                modifications 
                |> Array.map (fun x -> x.BonusAttackRoll.OnHit)
                |> Array.groupBy (fun x -> x.BonusType)
                |> Array.map (fun (header,bonusArr) -> if header <> BonusTypes.Flat 
                                                       then bonusArr
                                                            |> Array.sortByDescending (fun x -> x.Value) 
                                                            |> fun x -> Array.head x
                                                            |> fun x -> x.Value
                                                       elif header = BonusTypes.Flat
                                                       then bonusArr
                                                            |> Array.map (fun x -> x.Value)
                                                            |> Array.sum
                                                       else failwith "Unrecognized Pattern of attackBoni in 'addBoniToAttack'"
                              )
                |> Array.sum

            /// complete bonus on crit confirmation attack roll = dice roll + Sum of all boni (getBonusToAttack) + critical hit confirmation roll specific boni
            let totalAttackCritBonus (modifications: AttackModification []) critConfirmationRoll totalBonusToAttack =
                let critSpecificBonus =
                    modifications
                    |> Array.map (fun x -> x.BonusAttackRoll.OnCrit)
                    |> Array.groupBy (fun x -> x.BonusType)
                    |> Array.map (fun (header,bonusArr) -> if header <> BonusTypes.Flat 
                                                           then bonusArr
                                                                |> Array.sortByDescending (fun x -> x.Value) 
                                                                |> fun x -> Array.head x
                                                                |> fun x -> x.Value
                                                           elif header = BonusTypes.Flat
                                                           then bonusArr
                                                                |> Array.map (fun x -> x.Value)
                                                                |> Array.sum
                                                           else failwith "Unrecognized Pattern of attackBoni in 'addBoniToAttack'"
                                  )
                    |> Array.sum
                critConfirmationRoll + totalBonusToAttack + critSpecificBonus
        
        module toDmg =
            
            /// calculates bonus on damage rolls due to the ability score used by the weapon and the related multiplied
            let addDamageMod (char: CharacterStats) (weapon: Weapon) (modifications: AttackModification []) =
                /// calculates stat changes due to modifications
                let getStatChangesToDmg =
                    modifications
                    |> Array.collect (fun x -> x.StatChanges)
                    |> Array.filter (fun statChange -> statChange.Attribute = weapon.Modifier.ToDmg)
                    |> Array.groupBy (fun statChange -> statChange.Bonustype)
                    |> Array.map (fun (useless,x) -> x)
                    |> Array.map (fun x -> Array.sortByDescending (fun statChange -> statChange.AttributeChange) x)
                    |> Array.map (fun x -> Array.head x)
                    |> Array.map (fun statChange -> statChange.AttributeChange)
                    |> Array.sum
                    |> float

                match weapon.Modifier.ToDmg with
                    | Strength      -> char.Strength
                    | Dexterity     -> char.Dexterity
                    | Constitution  -> char.Constitution
                    | Intelligence  -> char.Intelligence
                    | Wisdom        -> char.Wisdom
                    | Charisma      -> char.Charisma
                    | _             -> 0
                |> fun stat -> float stat + getStatChangesToDmg
                |> fun x -> (x-10.)/2.
                |> fun x -> x * weapon.Modifier.MultiplicatorOnDamage.Multiplicator |> floor |> int

            /// calculates size change and resizes weapon damage dice.
            let sizeAdjustedWeaponDamage (size: SizeType) (weapon: Weapon) (modifications: AttackModification [])=
                
                let startSize =
                    match size with
                        | Fine          -> 1
                        | Diminuitive   -> 2
                        | Tiny          -> 3
                        | Small         -> 4
                        | Medium        -> 5
                        | Large         -> 6
                        | Huge          -> 7
                        | Gargantuan    -> 8
                        | Colossal      -> 9

                let effectiveSize =

                    let changeSizeBy =
                        modifications
                        |> Array.map (fun x -> x.SizeChanges)
                        |> Array.groupBy (fun x -> x.SizeChangeBonustype)
                        |> Array.map (fun (header,bonusArr) -> if header <> BonusTypes.Flat 
                                                               then bonusArr
                                                                    |> Array.sortByDescending (fun x -> x.SizeChangeValue) 
                                                                    |> fun x -> Array.head x
                                                                    |> fun x -> x.SizeChangeValue
                                                               elif header = BonusTypes.Flat
                                                               then bonusArr
                                                                    |> Array.map (fun x -> x.SizeChangeValue)
                                                                    |> Array.sum
                                                               else failwith "Unrecognized Pattern of attackBoni in 'addBoniToAttack'" 
                                     )
                        |> Array.sum

                    (startSize + changeSizeBy)
                    |> fun x -> if x > 9 then 9
                                elif x < 1 then 1
                                else x

                let diceRow = 
                    [|(1,1);(1,2);(1,3);(1,4);(1,6);(1,8);(1,10);(2,6);(2,8);(3,6);(3,8);
                    (4,6);(4,8);(6,6);(6,8);(8,6);(8,8);(12,6);(12,8);(16,6);(16,8);(24,6);(24,8);(36,6);(36,8)|] 

                ///https://paizo.com/paizo/faq/v5748nruor1fm#v5748eaic9t3f
                let getSizeChange reCalcWeapon (startS: int) (modifiedS: int) =
                    let snowFlakeIncrease numberofdice (die: int) =
                        match numberofdice with 
                        | 1 -> 2,die
                        | _ -> (numberofdice + int (floor (float numberofdice)*(1./3.))), die
                    let snowFlakeDecrease numberofdice (die: int) =
                        match numberofdice with 
                        | 2 -> 1,die
                        | _ -> (numberofdice - int (floor (float numberofdice)*(1./3.))), die
                    let isEven x = (x % 2) = 0         
                    let isOdd x = (x % 2) = 1
                    let sizeDiff = modifiedS - startS
                    let decInc = if sizeDiff < 0 then (-1.)
                                 elif sizeDiff > 0 then (1.)
                                 else 0.
                    let adjustedDie = match reCalcWeapon.Damage.Die with
                                      | 2   -> reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                                      | 3   -> reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                                      | 4   -> match reCalcWeapon.Damage.NumberOfDie with
                                               | 1                                                       -> reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                                               | odd when isOdd reCalcWeapon.Damage.NumberOfDie = true   -> int (ceil (float reCalcWeapon.Damage.NumberOfDie/2.)), 6
                                               | even when isEven reCalcWeapon.Damage.NumberOfDie = true -> (reCalcWeapon.Damage.NumberOfDie/2), 8
                                               | _                                                       -> failwith "unknown combination for reCalcWeapon damage dice calculator accoringly to size; Error4"
                                      | 6   -> reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                                      | 8   -> reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                                      | 10  -> reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                                      | 12  -> (reCalcWeapon.Damage.NumberOfDie*2), 6
                                      | 20  -> (reCalcWeapon.Damage.NumberOfDie*2), 10
                                      | _   -> if reCalcWeapon.Damage.Die % 10 = 0
                                               then ((reCalcWeapon.Damage.Die / 10) * reCalcWeapon.Damage.NumberOfDie), 10
                                               elif reCalcWeapon.Damage.Die % 6 = 0
                                               then ((reCalcWeapon.Damage.Die / 6) * reCalcWeapon.Damage.NumberOfDie), 6
                                               elif reCalcWeapon.Damage.Die % 4 = 0 
                                               then ((reCalcWeapon.Damage.Die / 4) * reCalcWeapon.Damage.NumberOfDie), 4
                                               else reCalcWeapon.Damage.NumberOfDie, reCalcWeapon.Damage.Die
                    let adjustedDieNum = fst adjustedDie
                    let adjustedDietype = snd adjustedDie

                    let rec loopResizeWeapon (n:int) (nDice:int) (die:int) = 

                        let stepIncrease = if startS + (int decInc*n) < 5 || (nDice * die) < 6 
                                           then 1
                                           else 2
                        let stepDecrease = if startS + (int decInc*n) < 6 || (nDice * die) < 8 
                                           then 1
                                           else 2
                        let findRowPosition =
                            Array.tryFindIndex (fun (x,y) -> x = nDice && y = die) diceRow
                        if sizeDiff = 0 || n >= abs sizeDiff
                            then nDice, die
                            else findRowPosition
                                 |> fun x -> if (x.IsSome) 
                                             then match decInc with 
                                                  | dec when decInc < 0. -> if x.Value < 1 then diceRow.[0] else diceRow.[x.Value - stepDecrease]
                                                  | inc when decInc > 0. -> if x.Value > (diceRow.Length-3) then (snowFlakeIncrease nDice die) else diceRow.[x.Value + stepIncrease]
                                                  | _ -> failwith "unknown combination for reCalcWeapon damage dice calculator accoringly to size; Error1"
                                             elif x.IsSome = false 
                                             then match decInc with 
                                                  | dec when decInc < 0. -> snowFlakeDecrease nDice die
                                                  | inc when decInc > 0. -> snowFlakeIncrease nDice die
                                                  | _ -> failwith "unknown combination for reCalcWeapon damage dice calculator accoringly to size; Error2"
                                             else failwith "unknown combination for reCalcWeapon damage dice calculator accoringly to size; Error3"
                                 |> fun (nDie,die) -> loopResizeWeapon (n+1) nDie die

                    loopResizeWeapon 0 adjustedDieNum adjustedDietype
                    |> fun (n,die) -> createDamage n die reCalcWeapon.Damage.DamageType

                getSizeChange weapon startSize effectiveSize


        
