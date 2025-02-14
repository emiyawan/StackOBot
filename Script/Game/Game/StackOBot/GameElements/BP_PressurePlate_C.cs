﻿using System;
using Script.Common;
using Script.CoreUObject;
using Script.Engine;
using Script.Library;

namespace Script.Game.StackOBot.Blueprints.GameElements
{
    /*
     * The pressure plate react to all actor types,  not just the player.
     * As such, you can push a crate onto it and the plate will activate any game objects that reference it.
     * This also means more than one thing can be on the plate, so we need to check if an object has been removed.
     * We also need to ensure that the activation is only triggered once.
     */
    [IsOverride]
    public partial class BP_PressurePlate_C
    {
        /*
         * Create Dynamic Material and set initial color and emissive
         */
        [IsOverride]
        public override void UserConstructionScript()
        {
            var SourceMaterial =
                Unreal.LoadObject<UMaterialInstance>(this, "/Game/StackOBot/Environment/Materials/MI_Glow.MI_Glow");

            Mat = Plate.CreateDynamicMaterialInstance(0, SourceMaterial);

            Mat.SetVectorParameterValue("Color", new FLinearColor
            {
                R = 1.0f,
                G = 1.0f,
                B = 1.0f,
                A = 1.0f
            });

            Mat.SetScalarParameterValue("Emissive", 3.0f);
        }

        [IsOverride]
        public override void ReceiveBeginPlay()
        {
            // @TODO
            UserConstructionScript();

            Trigger.OnComponentBeginOverlap.Add(this, OnComponentBeginOverlap);

            Trigger.OnComponentEndOverlap.Add(this, OnComponentEndOverlap);

            MoveButton.TheTimeline.TimelinePostUpdateFunc.Unbind();

            MoveButton.TheTimeline.TimelinePostUpdateFunc.Bind(this, OnTimelinePostUpdateFunc);
        }

        [IsOverride]
        public override void ReceiveEndPlay(EEndPlayReason EndPlayReason)
        {
            Trigger.OnComponentBeginOverlap.RemoveAll(this);

            Trigger.OnComponentEndOverlap.RemoveAll(this);

            MoveButton.TheTimeline.TimelinePostUpdateFunc.Unbind();
        }

        /*
         * The box collision is the root component and overlaps nearly all.
         * So when this event is triggered we check if at least one actor overlaps.
         * In this way it can be a pawn, a crate or a skeletal mesh mesh in raddoll
         */
        private void OnComponentBeginOverlap(UPrimitiveComponent OverlappedComponent, AActor OtherActor,
            UPrimitiveComponent OtherComp, Int32 OtherBodyIndex, Boolean bFromSweep, FHitResult SweepResult)
        {
            Trigger.GetOverlappingActors(out var OverlappingActors);

            if (OverlappingActors.Num() >= 1)
            {
                /*
                 * Assure this is only triggered once, even if more actors begin overlap.
                 */
                if (bEnabled)
                {
                    /*
                     * Change the color when begin overlap
                     */
                    Mat.SetVectorParameterValue("Color", new FLinearColor
                    {
                        R = 0.225624f,
                        G = 0.980208f,
                        B = 0.0f,
                        A = 1.0f
                    });

                    /*
                     * Start the interaction in the component so other actors can react on it.
                     */
                    BP_InteractionComponent.StartInteraction();

                    /*
                     * Animate the plate up depending if something overlaps
                     */

                    MoveButton.Play();

                    bEnabled = false;
                }
            }
        }

        /*
         * When end overlap is triggered, check if something is still overlapping to ensure it only triggers when nothing is in the plate.
         * Then reset the do once gate so the next activation can be triggered.
         */
        private void OnComponentEndOverlap(UPrimitiveComponent OverlappedComponent, AActor OtherActor,
            UPrimitiveComponent OtherComp, Int32 OtherBodyIndex)
        {
            Trigger.GetOverlappingActors(out var OverlappingActors);

            if (OverlappingActors.Num() == 0)
            {
                bEnabled = true;

                /*
                 * Change the color when end overlap
                 */
                Mat.SetVectorParameterValue("Color", new FLinearColor
                {
                    R = 1.0f,
                    G = 1.0f,
                    B = 1.0f,
                    A = 1.0f
                });

                /*
                 * Stop the interaction in the component so other actors can react on it.
                 */
                BP_InteractionComponent.StopInteraction();

                /*
                 * Animate the plate down depending if something overlaps
                 */

                MoveButton.Reverse();
            }
        }

        private void OnTimelinePostUpdateFunc()
        {
            var ButtonMovement = MoveButton.TheTimeline.InterpFloats[0].FloatCurve
                .GetFloatValue(MoveButton.TheTimeline.Position);

            Plate.K2_SetRelativeLocation(
                new FVector
                {
                    X = 0.0f,
                    Y = 0.0f,
                    Z = ButtonMovement * -10.0f
                },
                false,
                out var SweepHitResult,
                false);
        }

        private Boolean bEnabled = true;
    }
}