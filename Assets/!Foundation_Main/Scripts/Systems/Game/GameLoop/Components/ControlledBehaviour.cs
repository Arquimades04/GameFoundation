using System;
using UnityEngine;

namespace Foundation.Systems.Game
{
    [System.Flags]
    public enum ExecutionPhases
    {
        None = 0,
        Update = 1 << 0,
        FixedUpdate = 1 << 1,
        LateUpdate = 1 << 2,
        All = Update | FixedUpdate | LateUpdate
    }
    /// <summary>
    /// Represents a behaviour that is managed and executed by the <see cref="GameController"/>.
    /// Only one instance of a <see cref="ControlledBehaviour"/> can be registered with the controller at a time.
    /// </summary>
    public class ControlledBehaviour : MonoBehaviour
    {
        #region PUBLIC PROPERTIES

        /// <summary>
        /// Determines the order in which this behaviour is executed relative to
        /// other registered controlled behaviours.
        /// Lower values are executed first.
        /// </summary>
        public int ExecutionOrder
        {
            get
            {
                if (!executionOrderCaptured)
                {
                    executionOrder = GetExecutionOrder();
                    executionOrderCaptured = true;
                }
                return executionOrder;
            }
        }

        public ExecutionPhases Phase => phase;

        public void SetExecutionPhase(ExecutionPhases newPhase)
        {
            if ((newPhase & ~ExecutionPhases.All) != 0)
                throw new ArgumentOutOfRangeException(nameof(newPhase));

            if (IsRegistered && newPhase != RegisteredPhase)
            {
                if (!phaseChangeWarningLogged)
                {
                    scheduler?.RaiseBehaviourError(
                        this,
                        $"Cannot change execution phases while registered. " +
                        $"Registered: {RegisteredPhase}; requested: {newPhase}.");

                    phaseChangeWarningLogged = true;
                }

                return;
            }

            phase = newPhase;
        }

        /// <summary>
        /// Notifies this behaviour that it has been registered with the controller.
        /// </summary>
        internal void NotifyRegistered() => this.OnRegistered();

        /// <summary>
        /// Notifies this behaviour that it has been unregistered from the controller.
        /// </summary>
        internal void NotifyUnregistered() => this.OnUnregistered();

        /// <summary>
        /// Determines whether this behaviour is excluded from controlled execution
        /// while remaining registered with the game controller.
        /// </summary>
        public virtual bool ExcludedFromExecution => false;

        // Exposes lifecycle state for diagnostics and development tools.

        /// <summary>
        /// Gets whether this behaviour has completed its initialization lifecycle.
        /// Debugging and testing purposes.
        /// </summary>
        public bool IsInitialized => this.initialized;

        /// <summary>
        /// Gets whether this behaviour is currently part of the controller's
        /// committed execution state.
        /// Debugging and testing purposes.
        /// </summary>
        public bool IsRegistered { get; internal set; }

        #endregion

        #region PRIVATE PROPERTIES
        private int executionOrder;
        private bool executionOrderCaptured;
        /// <summary>
        /// Supplies the execution order. Evaluated exactly once per instance.
        /// </summary>
        protected virtual int GetExecutionOrder() => 0;

        // Tracks whether initialization has already been completed.
        private bool initialized;

        private IBehaviourScheduler scheduler;

        private ExecutionPhases phase = ExecutionPhases.None;
        private bool phaseChangeWarningLogged;

        /// <summary>
        /// Phases captured at registration time. Remains constant while registered,
        /// regardless of changes to <see cref="Phase"/>.
        /// </summary>
        internal ExecutionPhases RegisteredPhase { get; private set; }

        internal void CaptureRegisteredPhase()
        {
            this.RegisteredPhase = this.Phase;
            this.phaseChangeWarningLogged = false;
        }
        #endregion

        #region UNITY CALLBACKS

        /// <summary>
        /// Establishes the behaviour's initialization state before it can participate
        /// in the controlled lifecycle.
        /// </summary>
        private void Awake()
        {
            this.Initialize();
        }

        /// <summary>
        /// Completes initialization once for the lifetime of this component instance.
        /// </summary>
        private void Initialize()
        {
            if (this.initialized)
                return;

            this.initialized = true;
            this.OnInitialize();
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Executes this behaviour during the controller's Update phase.
        /// </summary>
        public virtual void ControlledUpdate()
        {
        }

        /// <summary>
        /// Executes this behaviour during the controller's FixedUpdate phase.
        /// </summary>
        public virtual void ControlledFixedUpdate()
        {
        }

        /// <summary>
        /// Executes this behaviour during the controller's LateUpdate phase.
        /// </summary>
        public virtual void ControlledLateUpdate()
        {
        }

        /// <summary>
        /// Called when the game is paused or resumed.
        /// Override this method to handle pause state changes.
        /// </summary>
        public virtual bool GamePausedInternal(bool paused)
        {

            return paused;
        }
        #endregion

        #region PROTECTED METHODS

        /// <summary>
        /// Performs behaviour-specific initialization.
        /// Override this method to initialize state required by the controlled behaviour.
        /// </summary>
        protected virtual void OnInitialize()
        {
        }

        /// <summary>
        /// Performs behaviour-specific cleanup when the component is destroyed.
        /// Override this method to release resources or state owned by the behaviour.
        /// </summary>
        protected virtual void OnDeInitialize()
        {
        }

        protected virtual void OnEnable()
        {
            if (!GameEntryPoint.TryResolve(out scheduler))
            {
                Debug.LogError("IBehaviourScheduler is not available.", this);
                return;
            }

            scheduler.Register(this);
        }

        /// <summary>
        /// Called when the behaviour is registered with the controller.
        /// Override this method to perform setup that depends on registration.
        /// </summary>
        protected virtual void OnRegistered()
        {
        }


        /// <summary>
        /// Called when the behaviour is unregistered from the controller.
        /// Override this method to perform cleanup that depends on unregistration.
        /// </summary>
        protected virtual void OnUnregistered()
        {
        }

        protected virtual void OnDisable()
        {
            scheduler?.Unregister(this);
        }
        /// <summary>
        /// Removes the behaviour from the controller and completes its destruction lifecycle.
        /// </summary>
        protected virtual void OnDestroy()
        {

            if (this.IsRegistered)
            {
                scheduler?.RaiseBehaviourError(this, "Behaviour was destroyed without being properly unregistered.");
                scheduler?.Unregister(this);
            }

            if (!this.initialized)
                return;

            this.OnDeInitialize();
            this.initialized = false;
        }
        #endregion
    }
}