using System.Collections.Generic;
using RailSim.Core;
using RailSim.InputSystem;
using RailSim.Rendering;
using UnityEngine;

namespace RailSim.Gameplay
{
    public class RailGameController : MonoBehaviour
    {
        [SerializeField] private LevelDefinition[] levelDefinitions =
        {
            new LevelDefinition { displayName = "Туториал", resourcePath = "Levels/tutorial", planningTime = 15f, levelIndex = 0, threeStarTime = 8f, twoStarTime = 12f, randomizeSwitches = true },
            new LevelDefinition { displayName = "Развилка", resourcePath = "Levels/level_02", planningTime = 12f, levelIndex = 1, threeStarTime = 10f, twoStarTime = 15f, randomizeSwitches = true },
            new LevelDefinition { displayName = "Мега-хаб", resourcePath = "Levels/mega_hub", planningTime = 10f, levelIndex = 2, threeStarTime = 15f, twoStarTime = 25f, randomizeSwitches = true },
            new LevelDefinition { displayName = "Эстакада", resourcePath = "Levels/level_04", planningTime = 20f, levelIndex = 3, threeStarTime = 12f, twoStarTime = 20f, randomizeSwitches = true }
        };
        [SerializeField] private float trackWidth = 0.22f;
        [SerializeField] private float switchVisualSize = 0.55f;
        [SerializeField] private float switchTapRadius = 0.8f;
        [SerializeField] private float goalMarkerSize = 0.6f;
        [SerializeField] private float finishProximityRadius = 0.6f;
        [SerializeField] private float maxRunDuration = 30f;

        private LevelBlueprint _blueprint;
        private RailGraph _graph;
        private RailSimulation _simulation;
        private readonly Dictionary<string, SwitchView> _switchViews = new();
        private readonly List<TrackView> _trackViews = new();
        private readonly List<TrainView> _trainViews = new();
        private GestureInput _gestureInput;
        private CameraPanController _cameraPan;
        private GameHud _hud;
        private GameMenu _menu;
        private LevelDefinition _currentLevel;
        private string _currentLevelPath;
        private Transform _trackRoot;
        private Transform _switchRoot;
        private Transform _trainRoot;
        private Transform _markerRoot;

        private float _planningTimer;
        private float _runTimer;
        private GamePhase _phase = GamePhase.Boot;
        private bool _goalReached;
        private bool _collisionOccurred;
        private int _trainsCompleted;
        private int _trainsTotal;
        private readonly List<FinishView> _finishViews = new();
        private readonly List<RailNode> _finishNodes = new();
        private readonly List<BonusView> _bonusViews = new();
        private bool _timeoutTriggered;
        private int _collectedBonusStars;

        private void Awake()
        {
            SetupCamera();
            CreateSceneRoots();
            CreateBackground();
            _gestureInput = gameObject.AddComponent<GestureInput>();
            _gestureInput.TapPerformed += HandleTap;

            _cameraPan = Camera.main.gameObject.GetComponent<CameraPanController>();
            if (_cameraPan == null)
            {
                _cameraPan = Camera.main.gameObject.AddComponent<CameraPanController>();
            }
            _cameraPan.BindInput(_gestureInput);

            _hud = GameHud.Create(Camera.main);
            _hud.SetMenuCallback(ReturnToMenu);
            _menu = GameMenu.Create(Camera.main, this, levelDefinitions.Length > 0 ? levelDefinitions[0].displayName : "Уровень");
            _menu.SetLevelDefinitions(levelDefinitions);
            _menu.ShowMainMenu();
        }

        private void CreateBackground()
        {
            AnimatedBackground.Create();
        }

        private void Start() {}

        private void Update()
        {
            if (_phase == GamePhase.Planning)
            {
                _planningTimer = Mathf.Max(0f, _planningTimer - Time.deltaTime);
                if (_planningTimer <= 0f)
                {
                    BeginSimulation();
                }
            }
            else if (_phase == GamePhase.Running)
            {
                _runTimer += Time.deltaTime;
                if (!_timeoutTriggered && _runTimer >= maxRunDuration)
                {
                    _timeoutTriggered = true;
                    _collisionOccurred = true;
                    _hud.UpdateStatus("ВРЕМЯ ИСТЕКЛО");
                }

                _simulation.Update(Time.deltaTime * _blueprint.simulationSpeed, _blueprint.goalNodeId);
                DetectFinishProximity();
                if (_goalReached && !_collisionOccurred)
                {
                    HandleWin();
                }
                else if (_collisionOccurred)
                {
                    HandleLose(_timeoutTriggered ? "ВРЕМЯ ИСТЕКЛО" : "СТОЛКНОВЕНИЕ");
                }
            }

            var timerValue = _phase == GamePhase.Planning ? _planningTimer : _runTimer;
            _hud.UpdateTimer(timerValue, _phase == GamePhase.Planning);
        }

        private void LoadLevel(LevelDefinition definition)
        {
            _currentLevel = definition ?? (levelDefinitions.Length > 0 ? levelDefinitions[0] : null);
            var resourcePath = _currentLevel?.resourcePath ?? "Levels/mega_hub";

            var textAsset = Resources.Load<TextAsset>(resourcePath);
            if (textAsset == null)
            {
                Debug.LogError($"Unable to find level at Resources/{resourcePath}");
                return;
            }

            _currentLevelPath = resourcePath;
            _blueprint = LevelBlueprintLoader.FromJson(textAsset.text);
            BuildGraph(_blueprint);
            BuildVisuals();

            var planning = _currentLevel != null && _currentLevel.planningTime > 0f
                ? _currentLevel.planningTime
                : _blueprint.planningTime;
            _planningTimer = planning;
            _runTimer = 0f;
            _phase = GamePhase.Planning;
            _goalReached = false;
            _collisionOccurred = false;
            _trainsCompleted = 0;
            _trainsTotal = _simulation?.Trains.Count ?? 0;
            _hud.UpdateStatus("ПЛАНИРУЙ");
            _timeoutTriggered = false;
            _menu?.Hide();
        }

        private void BuildGraph(LevelBlueprint blueprint)
        {
            if (_simulation != null)
            {
                _simulation.TrainReachedGoal -= HandleTrainReachedGoal;
                _simulation.CollisionDetected -= HandleCollision;
                _simulation.DeadEndReached -= HandleDeadEnd;
                _simulation.WrongSwitchEntry -= HandleWrongSwitchEntry;
                _simulation.EdgeBroken -= HandleEdgeBroken;
            }

            _graph = new RailGraph();
            foreach (var node in blueprint.nodes)
            {
                _graph.AddNode(node);
            }

            foreach (var edge in blueprint.edges)
            {
                _graph.AddEdge(edge);
            }

            var switches = new Dictionary<string, RailSwitchState>();
            var shouldRandomize = _currentLevel?.randomizeSwitches ?? false;
            
            foreach (var sw in blueprint.switches)
            {
                var state = new RailSwitchState(sw);
                
                // Randomize switch position if enabled
                if (shouldRandomize && sw.neighborCycle != null && sw.neighborCycle.Length > 1)
                {
                    var randomTimes = Random.Range(0, sw.neighborCycle.Length);
                    for (var i = 0; i < randomTimes; i++)
                    {
                        state.Toggle();
                    }
                }
                
                switches[sw.nodeId] = state;
            }

            _simulation = new RailSimulation(_graph, switches);
            _simulation.SpawnTrains(blueprint.trains);
            _simulation.SpawnBonuses(blueprint.bonuses ?? System.Array.Empty<BonusBlueprint>());
            _simulation.TrainReachedGoal += HandleTrainReachedGoal;
            _simulation.CollisionDetected += HandleCollision;
            _simulation.DeadEndReached += HandleDeadEnd;
            _simulation.WrongSwitchEntry += HandleWrongSwitchEntry;
            _simulation.EdgeBroken += HandleEdgeBroken;
            _simulation.BonusCollected += HandleBonusCollected;
            _trainsTotal = _simulation.Trains.Count;
            _collectedBonusStars = 0;
        }
        
        private void HandleDeadEnd(TrainRuntime train)
        {
            _collisionOccurred = true;
            _hud.UpdateStatus("ТУПИК!");
        }

        private void HandleWrongSwitchEntry(TrainRuntime train, RailNode node)
        {
            _collisionOccurred = true;
            _hud.UpdateStatus("СХОД С РЕЛЬС!");
            CreateExplosion(train.GetWorldPosition());
        }

        private void HandleEdgeBroken(RailEdge edge, Vector3 position)
        {
            // Find and update the track view
            foreach (var trackView in _trackViews)
            {
                if (trackView.Edge == edge)
                {
                    trackView.MarkBroken();
                    break;
                }
            }
            
            // Create explosion effect
            CreateExplosion(position);
        }

        private void CreateExplosion(Vector3 position)
        {
            var explosionGo = new GameObject("Explosion");
            explosionGo.transform.position = position;
            explosionGo.AddComponent<ExplosionEffect>();
        }

        private void HandleBonusCollected(BonusRuntime bonus, TrainRuntime train)
        {
            _collectedBonusStars += bonus.StarValue;
            _hud.UpdateStatus($"БОНУС! +{bonus.StarValue}⭐");
        }

        private void ClearVisuals()
        {
            foreach (var view in _trackViews)
            {
                Destroy(view.gameObject);
            }
            _trackViews.Clear();

            foreach (var view in _switchViews.Values)
            {
                Destroy(view.gameObject);
            }
            _switchViews.Clear();

            foreach (var view in _trainViews)
            {
                Destroy(view.gameObject);
            }
            _trainViews.Clear();

            foreach (var finish in _finishViews)
            {
                if (finish != null)
                {
                    Destroy(finish.gameObject);
                }
            }
            _finishViews.Clear();
            _finishNodes.Clear();
            
            foreach (var bonus in _bonusViews)
            {
                if (bonus != null)
                {
                    Destroy(bonus.gameObject);
                }
            }
            _bonusViews.Clear();
            
            _hud?.SetFinishTarget(null);
            
            // Reset train color index
            TrainView.ResetColorIndex();
        }

        private void BuildVisuals()
        {
            ClearVisuals();

            foreach (var edgeBlueprint in _blueprint.edges)
            {
                var railEdge = _graph.GetEdge(edgeBlueprint.id) ?? FindEdge(edgeBlueprint);
                if (railEdge == null)
                {
                    continue;
                }

                var go = new GameObject($"Track_{edgeBlueprint.id}");
                go.transform.SetParent(_trackRoot, false);
                var view = go.AddComponent<TrackView>();
                view.Bind(railEdge, trackWidth);
                _trackViews.Add(view);
            }

            foreach (var switchState in _simulation.Switches)
            {
                if (_graph.GetNode(switchState.Key) is not { } node)
                {
                    continue;
                }

                var go = new GameObject($"Switch_{switchState.Key}");
                go.transform.SetParent(_switchRoot, false);
                go.transform.position = node.WorldPosition;
                var collider = go.AddComponent<CircleCollider2D>();
                collider.radius = Mathf.Max(0.1f, switchTapRadius * 0.5f);
                var view = go.AddComponent<SwitchView>();
                view.Initialize(node, _graph);
                view.Bind(switchState.Value);
                view.SetVisualSize(switchVisualSize);
                _switchViews[switchState.Key] = view;
            }

            foreach (var train in _simulation.Trains)
            {
                var go = new GameObject($"Train_{train.Blueprint.id}");
                go.transform.SetParent(_trainRoot, false);
                var view = go.AddComponent<TrainView>();
                view.Bind(train);
                _trainViews.Add(view);
            }

            foreach (var node in _graph.Nodes.Values)
            {
                if (node.Type != NodeType.Finish)
                {
                    continue;
                }

                var go = new GameObject($"Finish_{node.Id}");
                go.transform.SetParent(_markerRoot, false);
                go.transform.position = node.WorldPosition + new Vector3(0f, 0.05f, 0f);
                var marker = go.AddComponent<FinishView>();
                marker.Bind(node, this, goalMarkerSize);
                _finishViews.Add(marker);
                _finishNodes.Add(node);
            }

            if (_finishNodes.Count > 0)
            {
                _hud?.SetFinishTarget(_finishNodes[0].WorldPosition);
            }
            else
            {
                _hud?.SetFinishTarget(null);
            }

            // Create bonus views
            foreach (var bonus in _simulation.Bonuses)
            {
                var go = new GameObject($"Bonus_{bonus.Id}");
                go.transform.SetParent(_markerRoot, false);
                var view = go.AddComponent<BonusView>();
                view.Initialize(bonus);
                _bonusViews.Add(view);
            }

            // Set camera bounds based on all nodes
            if (_cameraPan != null && _graph.Nodes.Count > 0)
            {
                var positions = new Vector3[_graph.Nodes.Count];
                var i = 0;
                foreach (var node in _graph.Nodes.Values)
                {
                    positions[i++] = node.WorldPosition;
                }
                _cameraPan.SetBoundsFromNodes(positions);
            }
        }

        public void RequestStart(LevelDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogWarning("Level definition is missing.");
                return;
            }

            if (_phase == GamePhase.Running || _phase == GamePhase.Planning)
            {
                return;
            }

            LoadLevel(definition);
        }

        public void RequestRestart()
        {
            if (_currentLevel != null)
            {
                LoadLevel(_currentLevel);
            }
            else if (levelDefinitions.Length > 0)
            {
                LoadLevel(levelDefinitions[0]);
            }
        }

        public void ReturnToMenu()
        {
            ClearVisuals();
            if (_simulation != null)
            {
                _simulation.TrainReachedGoal -= HandleTrainReachedGoal;
                _simulation.CollisionDetected -= HandleCollision;
                _simulation.DeadEndReached -= HandleDeadEnd;
                _simulation.WrongSwitchEntry -= HandleWrongSwitchEntry;
                _simulation.EdgeBroken -= HandleEdgeBroken;
                _simulation.BonusCollected -= HandleBonusCollected;
            }
            _simulation = null;
            _currentLevel = null;
            _currentLevelPath = null;
            _goalReached = false;
            _collisionOccurred = false;
            _planningTimer = 0f;
            _runTimer = 0f;
            _phase = GamePhase.Boot;
            _timeoutTriggered = false;
            _hud.UpdateStatus("МЕНЮ");
            _hud.UpdateTimer(0f, true);
            _hud.SetFinishTarget(null);
            _menu.ShowMainMenu();
        }

        private RailEdge FindEdge(EdgeBlueprint blueprint)
        {
            var from = _graph.GetNode(blueprint.fromNodeId);
            var to = _graph.GetNode(blueprint.toNodeId);
            if (from == null || to == null)
            {
                return null;
            }

            foreach (var neighbor in from.Neighbors)
            {
                if (neighbor.Node == to)
                {
                    return neighbor.Edge;
                }
            }

            return null;
        }

        private void BeginSimulation()
        {
            if (_phase != GamePhase.Planning)
            {
                return;
            }

            _phase = GamePhase.Running;
            _runTimer = 0f;
            _hud.UpdateStatus("ПОЕЗДЫ В ПУТИ");
        }

        private bool TryToggleNearestSwitch(Vector2 worldPoint)
        {
            if (_simulation == null || _switchViews.Count == 0)
            {
                return false;
            }

            SwitchView closest = null;
            var bestDistance = switchTapRadius;

            foreach (var view in _switchViews.Values)
            {
                var distance = Vector2.Distance(worldPoint, view.transform.position);
                if (distance <= bestDistance)
                {
                    bestDistance = distance;
                    closest = view;
                }
            }

            if (closest == null)
            {
                return false;
            }

            if (_phase != GamePhase.Planning && _phase != GamePhase.Running)
            {
                return false;
            }

            _simulation.ToggleSwitch(closest.NodeId);
            closest.UpdateVisual();
            closest.Pulse();
            return true;
        }

        private void HandleTap(Vector2 screenPosition)
        {
            if (_phase == GamePhase.Boot)
            {
                return;
            }

            if (Camera.main == null)
            {
                return;
            }

            var world3 = Camera.main.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            var world = (Vector2)world3;
            if (TryToggleNearestSwitch(world))
            {
                return;
            }
            else if (_phase == GamePhase.Planning)
            {
                BeginSimulation();
            }
            else if (_phase == GamePhase.Win)
            {
                _menu.ShowMainMenu();
            }
            else if (_phase == GamePhase.Lose)
            {
                RequestRestart();
            }
        }

        public void HandleFinishContact(RailNode node, TrainView trainView)
        {
            if (_phase != GamePhase.Running || _simulation == null)
            {
                return;
            }

            var runtime = trainView != null ? trainView.Runtime : null;
            _simulation.ForceCompleteTrain(runtime, node);
        }

        private void DetectFinishProximity()
        {
            if (_simulation == null || _finishNodes.Count == 0)
            {
                return;
            }

            foreach (var train in _simulation.Trains)
            {
                if (train.HasFinished)
                {
                    continue;
                }

                var position = train.GetWorldPosition();
                foreach (var node in _finishNodes)
                {
                    if (Vector3.Distance(position, node.WorldPosition) <= finishProximityRadius)
                    {
                        _simulation.ForceCompleteTrain(train, node);
                        break;
                    }
                }
            }
        }

        private void SetupCamera()
        {
            if (Camera.main == null)
            {
                var cameraGo = new GameObject("Main Camera");
                var cam = cameraGo.AddComponent<Camera>();
                cam.tag = "MainCamera";
                cam.transform.position = new Vector3(0f, 0f, -10f);
            }
            else
            {
                Camera.main.transform.position = new Vector3(0f, 0f, -10f);
            }
        }

        private void CreateSceneRoots()
        {
            _trackRoot = CreateRoot("Tracks");
            _switchRoot = CreateRoot("Switches");
            _trainRoot = CreateRoot("Trains");
            _markerRoot = CreateRoot("Markers");
        }

        private Transform CreateRoot(string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            return go.transform;
        }

        private void HandleTrainReachedGoal(TrainRuntime runtime, RailNode _)
        {
            _trainsCompleted++;
            if (_trainsCompleted >= _trainsTotal && _trainsTotal > 0)
            {
                _goalReached = true;
            }
        }

        private void HandleCollision(TrainRuntime _, TrainRuntime __)
        {
            _collisionOccurred = true;
        }

        private void HandleWin()
        {
            if (_phase == GamePhase.Win)
            {
                return;
            }

            _phase = GamePhase.Win;
            
            // Calculate stars based on completion time
            var stars = 1;
            if (_currentLevel != null)
            {
                stars = LevelProgress.CalculateStars(_runTimer, _currentLevel.threeStarTime, _currentLevel.twoStarTime);
                LevelProgress.SetStars(_currentLevel.levelIndex, stars);
            }
            
            var starText = new string('⭐', stars);
            _hud.UpdateStatus($"ПОБЕДА! {starText}");
            
            // Find next level
            LevelDefinition nextLevel = null;
            if (_currentLevel != null)
            {
                var currentIndex = System.Array.FindIndex(levelDefinitions, l => l == _currentLevel);
                if (currentIndex >= 0 && currentIndex < levelDefinitions.Length - 1)
                {
                    nextLevel = levelDefinitions[currentIndex + 1];
                }
            }
            
            _menu.ShowWinMenu(nextLevel, stars, _runTimer);
        }

        public void RequestNextLevel(LevelDefinition nextLevel)
        {
            if (nextLevel == null)
            {
                ReturnToMenu();
                return;
            }
            
            _phase = GamePhase.Boot;
            LoadLevel(nextLevel);
        }

        private void HandleLose(string message)
        {
            if (_phase == GamePhase.Lose)
            {
                return;
            }

            _phase = GamePhase.Lose;
            _hud.UpdateStatus(message);
            _menu.ShowRestartMenu($"{message}. ПОПРОБУЙ СНОВА.");
        }

        private void OnDestroy()
        {
            if (_gestureInput != null)
            {
                _gestureInput.TapPerformed -= HandleTap;
            }

            if (_cameraPan != null)
            {
                _cameraPan.BindInput(null);
            }

            if (_simulation != null)
            {
                _simulation.TrainReachedGoal -= HandleTrainReachedGoal;
                _simulation.CollisionDetected -= HandleCollision;
                _simulation.DeadEndReached -= HandleDeadEnd;
                _simulation.WrongSwitchEntry -= HandleWrongSwitchEntry;
                _simulation.EdgeBroken -= HandleEdgeBroken;
                _simulation.BonusCollected -= HandleBonusCollected;
            }

            if (_hud != null)
            {
                Destroy(_hud.gameObject);
            }
        }
    }

    [System.Serializable]
    public class LevelDefinition
    {
        public string displayName = "Уровень";
        public string resourcePath = "Levels/tutorial";
        public float planningTime = 10f;
        public int levelIndex;
        public float threeStarTime = 5f;  // Complete in this time for 3 stars
        public float twoStarTime = 10f;   // Complete in this time for 2 stars
        public bool randomizeSwitches = true;
    }

    public enum GamePhase
    {
        Boot,
        Planning,
        Running,
        Win,
        Lose
    }

    public static class LevelProgress
    {
        private const string CompletedLevelsKey = "CompletedLevels";
        private const string StarsKey = "LevelStars_";
        private const string TotalStarsKey = "TotalStars";

        public static bool IsLevelCompleted(int levelIndex)
        {
            return GetStars(levelIndex) > 0;
        }

        public static int GetStars(int levelIndex)
        {
            return PlayerPrefs.GetInt(StarsKey + levelIndex, 0);
        }

        public static void SetStars(int levelIndex, int stars)
        {
            var currentStars = GetStars(levelIndex);
            if (stars > currentStars)
            {
                PlayerPrefs.SetInt(StarsKey + levelIndex, stars);
                RecalculateTotalStars();
                PlayerPrefs.Save();
            }
        }

        public static int GetTotalStars()
        {
            return PlayerPrefs.GetInt(TotalStarsKey, 0);
        }

        private static void RecalculateTotalStars()
        {
            var total = 0;
            for (var i = 0; i < 100; i++)
            {
                total += GetStars(i);
            }
            PlayerPrefs.SetInt(TotalStarsKey, total);
        }

        public static void MarkLevelCompleted(int levelIndex)
        {
            if (GetStars(levelIndex) == 0)
            {
                SetStars(levelIndex, 1);
            }
        }

        public static int CalculateStars(float completionTime, float threeStarTime, float twoStarTime)
        {
            if (completionTime <= threeStarTime) return 3;
            if (completionTime <= twoStarTime) return 2;
            return 1;
        }

        public static void ResetProgress()
        {
            for (var i = 0; i < 100; i++)
            {
                PlayerPrefs.DeleteKey(StarsKey + i);
            }
            PlayerPrefs.DeleteKey(TotalStarsKey);
            PlayerPrefs.DeleteKey(CompletedLevelsKey);
            PlayerPrefs.Save();
        }
    }
}

