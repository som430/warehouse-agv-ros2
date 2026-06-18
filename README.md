# Warehouse AGV Simulation

Autonomous Ground Vehicle simulation using ROS2, LiDAR SLAM, and Nav2 — with a C# WebSocket control client.

## Tech Stack

- ROS2 Humble
- SLAM Toolbox / Cartographer
- Nav2 (Navigation2)
- Gazebo
- C# (.NET 8) + rosbridge WebSocket
- C++ / Python ROS2 nodes

## Architecture
[Gazebo Simulation]

|

[LiDAR /scan]

|

[SLAM Toolbox] → [/map]

|

[Nav2 Stack] ← [Waypoint Commands]

|

[rosbridge WebSocket :9090]

|

[C# AGV Client] → publishes /cmd_vel

→ subscribes /odom (real-time position)

## Features

- 2D LiDAR-based mapping in custom warehouse environment
- Autonomous multi-waypoint navigation via Nav2
- C++ velocity publisher node for direct motor control
- Python waypoint follower using NavigateToPose action client
- C# control client over rosbridge WebSocket (TCP/IP)

## Run

**1. Launch simulation**
```bash
ros2 launch turtlebot3_gazebo turtlebot3_world.launch.py
```

**2. Launch Nav2 with saved map**
```bash
ros2 launch turtlebot3_navigation2 navigation2.launch.py \
  use_sim_time:=True map:=$HOME/warehouse_map.yaml
```

**3. Launch rosbridge**
```bash
ros2 launch rosbridge_server rosbridge_websocket_launch.xml
```

**4. Run waypoint follower**
```bash
ros2 run agv_controller waypoint_follower.py
```

**5. Run C# client**
```bash
cd ~/AgvClient && dotnet run
```
