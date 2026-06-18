#!/usr/bin/env python3
import rclpy
from rclpy.node import Node
from rclpy.action import ActionClient
from nav2_msgs.action import NavigateToPose
from geometry_msgs.msg import PoseStamped

class WaypointFollower(Node):
    def __init__(self):
        super().__init__('waypoint_follower')
        self._client = ActionClient(self, NavigateToPose, 'navigate_to_pose')

        # 웨이포인트 3개 정의 (x, y 좌표)
        self.waypoints = [
            (0.5, 0.5),
            (-0.5, 0.5),
            (0.0, 0.0),
        ]
        self.current = 0

    def send_next_goal(self):
        if self.current >= len(self.waypoints):
            self.get_logger().info('모든 웨이포인트 완료!')
            return

        x, y = self.waypoints[self.current]
        goal = NavigateToPose.Goal()
        goal.pose = PoseStamped()
        goal.pose.header.frame_id = 'map'
        goal.pose.header.stamp = self.get_clock().now().to_msg()
        goal.pose.pose.position.x = x
        goal.pose.pose.position.y = y
        goal.pose.pose.orientation.w = 1.0

        self.get_logger().info(f'목적지 {self.current + 1}: ({x}, {y})')
        self._client.wait_for_server()
        future = self._client.send_goal_async(goal)
        future.add_done_callback(self.goal_response_callback)

    def goal_response_callback(self, future):
        handle = future.result()
        result_future = handle.get_result_async()
        result_future.add_done_callback(self.result_callback)

    def result_callback(self, future):
        self.get_logger().info(f'웨이포인트 {self.current + 1} 도착!')
        self.current += 1
        self.send_next_goal()

def main():
    rclpy.init()
    node = WaypointFollower()
    node.send_next_goal()
    rclpy.spin(node)

if __name__ == '__main__':
    main()
