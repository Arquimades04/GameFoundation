using System.Collections.Generic;
using UnityEngine;
using System;


namespace Foundation.Systems.Game
{
    #region SERVICE MANAGEMENT
    public interface IBehaviourScheduler
    {
        public void Register(ControlledBehaviour behaviour);
        public void Unregister(ControlledBehaviour behaviour);
        public void RaiseBehaviourError(ControlledBehaviour behaviour, string message);
    }
    #endregion

    /// <summary>
    /// Coordinates the execution and lifecycle of registered <see cref="ControlledBehaviour"/> instances.
    /// </summary>
    public sealed class GameController : MonoBehaviour, IBehaviourScheduler, IGameService
    {
        #region PUBLIC PROPERTIES
        /// <summary>
        /// Gets whether controlled behaviour execution is enabled. Debugging and testing purposes.
        /// </summary>
        public bool IsLoopEnabled => this.loopEnabled;
        #endregion

        #region PRIVATE PROPERTIES
        // Maintains the committed registration state and execution order.
        private HashSet<ControlledBehaviour> registeredBehaviours = new HashSet<ControlledBehaviour>();
        private List<ControlledBehaviour> controlledUpdateBehaviours = new List<ControlledBehaviour>();
        private List<ControlledBehaviour> controlledFixedUpdateBehaviours = new List<ControlledBehaviour>();
        private List<ControlledBehaviour> controlledLateUpdateBehaviours = new List<ControlledBehaviour>();

        // Controls whether the execution loops are allowed to run.
        private bool loopEnabled = true;
        // Controls whether the execution loops are paused due to a global pause request or disabled loop.
        private bool executionEnabled => initialized && loopEnabled && !pauseController.IsPaused;

        // Registration changes are deferred so the execution list remains stable
        // for the duration of each execution phase.
        private readonly HashSet<ControlledBehaviour> pendingRegistrations = new();
        private readonly List<ControlledBehaviour> pendingRegistrationOrder = new();
        private readonly HashSet<ControlledBehaviour> pendingUnregistrations = new();
        private readonly List<ControlledBehaviour> registrationBatch = new();
        private readonly List<ControlledBehaviour> unregistrationBatch = new();

        // Synchronization lock for thread-safe access to registration and execution state.
        private readonly object syncLock = new object();

        // Tracks the registration order for controlled behaviours.
        private readonly Dictionary<ControlledBehaviour, long> registrationOrder = new();
        // Registration order for controlled behaviours to determine execution sequence in equal execution order.
        private long nextRegistrationOrder;
        #endregion

        #region SERVICE MANAGEMENT IMPLEMENTATION

        private PauseController pauseController;
        private DebugController debugController;
        private bool initialized;
        private bool shutdown;
        public void Initialize(PauseController pause, DebugController debug)
        {
            if (initialized) throw new InvalidOperationException("GameController is already initialized.");

            if (debug == null)
                throw new ArgumentNullException(nameof(debug));
            if (pause == null)
                throw new ArgumentNullException(nameof(pause));

            pauseController = pause;
            debugController = debug;
            initialized = true;

            SetLoopsEnabled(true);

            debugController.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "GameController Initialized", "GameController");
        }

        public void Shutdown()
        {
            if (!initialized || shutdown) return;

            shutdown = true;

            SetLoopsEnabled(false);
            ClearControlledBehaviours();

            initialized = false;
            pauseController = null;

            debugController?.Log(DebugController.DebugCategory.General, DebugController.DebugPersistence.RuntimeOnly, "GameController Shutdown", "GameController");
            debugController = null;
        }
        #endregion

        #region UNITY CALLBACKS
        /// <summary>
        /// Executes the update loop for all registered controlled behaviours.
        /// Also synchronizes pending registrations/unregistrations at the start of the frame to maintain a stable execution list.
        /// </summary>
        private void Update()
        {
            if (!initialized) return;
            Synchronize();

            if (!this.executionEnabled) return;

            this.ExecuteUpdate();
        }
        /// <summary>
        /// Executes the fixed update loop for all registered controlled behaviours.
        /// </summary>
        private void FixedUpdate()
        {
            if (!initialized) return;
            if (!this.executionEnabled) return;
            this.ExecuteFixedUpdate();
        }
        /// <summary>
        /// Executes the late update loop for all registered controlled behaviours.
        /// </summary>
        private void LateUpdate()
        {
            if (!initialized) return;
            if (!this.executionEnabled) return;
            this.ExecuteLateUpdate();
        }
        #endregion

        #region PUBLIC METHODS
        /// <summary>
        /// Enables or disables execution of controlled behaviour loops.
        /// </summary>
        /// <param name="enable">
        /// True to allow controlled behaviour execution; false to pause it.
        /// </param>
        public void SetLoopsEnabled(bool enable)
        {
            this.loopEnabled = enable;
        }

        /// <summary>
        /// Queues a controlled behaviour for registration at the next synchronization point.
        /// </summary>
        /// <param name="behaviour">
        /// The behaviour to register.
        /// </param>
        public void Register(ControlledBehaviour behaviour)
        {
            if (!initialized || shutdown) return;

            if (behaviour == null)
            {
                debugController.LogWarning(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                "Attempted to register a null behaviour",
                "GameController");
                return;
            }

            lock (syncLock)
            {
                pendingUnregistrations.Remove(behaviour);
                if (pendingRegistrations.Add(behaviour))
                    pendingRegistrationOrder.Add(behaviour);
            }
        }

        /// <summary>
        /// Queues a controlled behaviour for removal at the next synchronization point.
        /// </summary>
        /// <param name="behaviour">
        /// The behaviour to unregister.
        /// </param>
        public void Unregister(ControlledBehaviour behaviour)
        {
            if (!initialized || shutdown) return;
            if (behaviour == null)
            {
                debugController.LogWarning(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                "Attempted to unregister a null behaviour",
                "GameController");
                return;
            }
            lock (syncLock)
            {
                if (pendingRegistrations.Remove(behaviour))
                    pendingRegistrationOrder.Remove(behaviour);

                pendingUnregistrations.Add(behaviour);
            }
        }

        public void RaiseBehaviourError(ControlledBehaviour behaviour, string message)
        {
            if (behaviour == null)
                return;

            debugController.LogError(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                message,
                "GameController");
        }
        #endregion

        #region PRIVATE METHODS
        /// <summary>
        /// Executes the controlled Update phase.
        /// </summary>
        private void ExecuteUpdate()
        {
            if (!this.executionEnabled) return;

            foreach (ControlledBehaviour controlledBehaviour in this.controlledUpdateBehaviours)
            {
                // Check if the controlledBehaviour is null
                if (controlledBehaviour == null)
                    continue;

                if (controlledBehaviour.ExcludedFromExecution)
                    continue;

                if (!controlledBehaviour.isActiveAndEnabled)
                    continue;

                controlledBehaviour.ControlledUpdate();
            }
        }
        /// <summary>
        /// Executes the controlled FixedUpdate phase.
        /// </summary>
        private void ExecuteFixedUpdate()
        {
            if (!this.executionEnabled) return;

            foreach (ControlledBehaviour controlledBehaviour in this.controlledFixedUpdateBehaviours)
            {
                // Check if the controlledBehaviour is null
                if (controlledBehaviour == null)
                    continue;

                if (controlledBehaviour.ExcludedFromExecution)
                    continue;

                if (!controlledBehaviour.isActiveAndEnabled)
                    continue;

                controlledBehaviour.ControlledFixedUpdate();
            }
        }
        /// <summary>
        /// Executes the controlled LateUpdate phase.
        /// </summary>
        private void ExecuteLateUpdate()
        {
            if (!this.executionEnabled) return;

            foreach (ControlledBehaviour controlledBehaviour in this.controlledLateUpdateBehaviours)
            {
                // Check if the controlledBehaviour is null
                if (controlledBehaviour == null)
                    continue;

                if (controlledBehaviour.ExcludedFromExecution)
                    continue;

                if (!controlledBehaviour.isActiveAndEnabled)
                    continue;

                controlledBehaviour.ControlledLateUpdate();
            }
        }


        /// <summary>
        /// Commits the current batch of deferred registration changes.
        /// 
        /// Pending requests are captured and cleared before being applied so that
        /// requests generated during synchronization are deferred to the next batch.
        /// </summary>
        private void Synchronize()
        {
            if (this.pendingRegistrations.Count == 0 &&
                this.pendingUnregistrations.Count == 0) return;

            lock (syncLock)
            {
                // Capture this synchronization batch.
                registrationBatch.Clear();
                unregistrationBatch.Clear();

                registrationBatch.AddRange(this.pendingRegistrationOrder);
                unregistrationBatch.AddRange(this.pendingUnregistrations);

                this.pendingRegistrations.Clear();
                this.pendingRegistrationOrder.Clear();
                this.pendingUnregistrations.Clear();
            }

            foreach (ControlledBehaviour behaviour in unregistrationBatch) UnregisterImmediate(behaviour);

            this.SortControlledBehaviours();

            foreach (ControlledBehaviour behaviour in registrationBatch) RegisterImmediate(behaviour);

            this.SortControlledBehaviours();
        }

        /// <summary>
        /// Immediately commits a behaviour to the controller's execution state.
        /// </summary>
        private void RegisterImmediate(ControlledBehaviour behaviour)
        {
            if (behaviour == null) return;

            if (!this.registeredBehaviours.Add(behaviour)) return;


            registrationOrder.Add(behaviour, nextRegistrationOrder++);

            behaviour.CaptureRegisteredPhase();
            ExecutionPhases phase = behaviour.RegisteredPhase;

            if ((phase & ExecutionPhases.Update) != 0)
                this.controlledUpdateBehaviours.Add(behaviour);

            if ((phase & ExecutionPhases.FixedUpdate) != 0)
                this.controlledFixedUpdateBehaviours.Add(behaviour);

            if ((phase & ExecutionPhases.LateUpdate) != 0)
                this.controlledLateUpdateBehaviours.Add(behaviour);

            behaviour.IsRegistered = true;


            try
            {
                behaviour.NotifyRegistered();
            }
            catch (Exception exception)
            {
                debugController.Log(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                    "Exception occurred while registering behaviour: " + behaviour.name + " - " + exception.Message,
                    "GameController");
            }

            debugController.Log(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                "Successfully Registered behaviour: " + behaviour.name,
                "GameController");
        }

        /// <summary>
        /// Immediately removes a behaviour from the controller's execution state.
        /// </summary>
        private void UnregisterImmediate(ControlledBehaviour behaviour)
        {
            if (ReferenceEquals(behaviour, null))
                return;

            bool wasDestroyed = behaviour == null;

            if (!this.registeredBehaviours.Remove(behaviour)) return;

            if ((behaviour.RegisteredPhase & ExecutionPhases.Update) != 0)
                this.controlledUpdateBehaviours.Remove(behaviour);

            if ((behaviour.RegisteredPhase & ExecutionPhases.FixedUpdate) != 0)
                this.controlledFixedUpdateBehaviours.Remove(behaviour);

            if ((behaviour.RegisteredPhase & ExecutionPhases.LateUpdate) != 0)
                this.controlledLateUpdateBehaviours.Remove(behaviour);

            registrationOrder.Remove(behaviour);

            behaviour.IsRegistered = false;

            try
            {
                behaviour.NotifyUnregistered();
            }
            catch (Exception exception)
            {
                debugController.Log(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                    "Exception occurred while unregistering behaviour: " + (wasDestroyed ? "<destroyed behaviour>" : behaviour.name) + " - " + exception.Message,
                    "GameController");
            }

            debugController.Log(DebugController.DebugCategory.Behaviour, DebugController.DebugPersistence.RuntimeOnly,
                "Successfully unregistered behaviour: " + (wasDestroyed ? "<destroyed behaviour>" : behaviour.name),
                "GameController");
        }

        /// <summary>
        /// Sorts the controlled behaviours based on their execution order.
        /// Lower execution order values are executed first.
        /// </summary>
        private void SortControlledBehaviours()
        {
            controlledUpdateBehaviours.Sort((ControlledBehaviour a, ControlledBehaviour b) =>
            {
                int byExecutionOrder = b.ExecutionOrder.CompareTo(a.ExecutionOrder);
                if (byExecutionOrder != 0) return byExecutionOrder;

                return registrationOrder[a].CompareTo(registrationOrder[b]);
            });

            controlledFixedUpdateBehaviours.Sort((ControlledBehaviour a, ControlledBehaviour b) =>
            {
                int byExecutionOrder = b.ExecutionOrder.CompareTo(a.ExecutionOrder);
                if (byExecutionOrder != 0) return byExecutionOrder;

                return registrationOrder[a].CompareTo(registrationOrder[b]);
            });

            controlledLateUpdateBehaviours.Sort((ControlledBehaviour a, ControlledBehaviour b) =>
            {
                int byExecutionOrder = b.ExecutionOrder.CompareTo(a.ExecutionOrder);
                if (byExecutionOrder != 0) return byExecutionOrder;

                return registrationOrder[a].CompareTo(registrationOrder[b]);
            });
        }

        private void ClearControlledBehaviours()
        {
            var snapshot = new List<ControlledBehaviour>(registeredBehaviours);

            foreach (var behaviour in snapshot) UnregisterImmediate(behaviour);

            registeredBehaviours.Clear();
            controlledUpdateBehaviours.Clear();
            controlledFixedUpdateBehaviours.Clear();
            controlledLateUpdateBehaviours.Clear();
            registrationOrder.Clear();

            lock (syncLock)
            {
                pendingRegistrations.Clear();
                pendingRegistrationOrder.Clear();
                pendingUnregistrations.Clear();
                registrationBatch.Clear();
                unregistrationBatch.Clear();
            }
        }
        #endregion
    }
}
