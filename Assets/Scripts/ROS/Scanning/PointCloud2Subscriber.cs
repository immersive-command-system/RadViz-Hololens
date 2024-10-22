﻿using ROSBridgeLib;
using SimpleJSON;
using System.Text;
using UnityEngine;

public class PointCloud2Subscriber : ROSBridgeSubscriber
{
    public static string rendererObjectName = "drone_model";

    private static bool verbose = false;

    public new static string GetMessageTopic()
    {
        return "velodyne/velodyne_points";
    }

    public new static string GetMessageType()
    {
        return "sensor_msgs/PointCloud2";
    }

    public new static ROSBridgeMsg ParseMessage(JSONNode msg)
    {
        return new PointCloud2Msg(msg);
    }

    public new static void CallBack(ROSBridgeMsg msg)
    {
        PointCloud2Msg pointCloudMsg = (PointCloud2Msg)msg;
        if (verbose)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pointCloudMsg.GetHeader().GetSeq());
            sb.Append(":\n");
            sb.AppendFormat("Size: {0} X {1} = {2}\n", pointCloudMsg.GetWidth(), pointCloudMsg.GetHeight(), pointCloudMsg.GetWidth() * pointCloudMsg.GetHeight());
            sb.Append(pointCloudMsg.GetFieldString());
            Debug.Log(sb.ToString());
        }

        LidarVisualizer visualizer = GameObject.Find(rendererObjectName).GetComponent<LidarVisualizer>();
        visualizer.SetPointCloud(pointCloudMsg.GetCloud());
    }
}
