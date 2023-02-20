using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public static class MinioUtils
{
    private static string minioHost = "10.11.10.147:9000";
    private static string accessKey = "I0bm6V8JZOgInurr";
    private static string secretKey = "o2KDKyzgRWc9GqlnTqhorqxfoAxR77Lp";

    public static UnityWebRequest CreateUploadFileRequest(string bucket, string filePath)
    {
        byte[] fileData = File.ReadAllBytes(filePath);

        string resource = $"/{bucket}/{System.IO.Path.GetFileName(filePath)}";
        string contentType = "application/octet-stream";
        string date = DateTime.UtcNow.ToString("R");

        // S3 signature
        string _signature = $"PUT\n\n{contentType}\n{date}\n{resource}";
        string authorization = GetAuthorizationHeader(_signature);

        // create request 
        UnityWebRequest www = UnityWebRequest.Put($"http://{minioHost}{resource}", fileData);
        www.SetRequestHeader("Host", minioHost);
        www.SetRequestHeader("Date", date);
        www.SetRequestHeader("Content-Type", contentType);
        www.SetRequestHeader("Authorization", authorization);

        return www;
    }

    private static string GetAuthorizationHeader(string signature)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(secretKey);
        byte[] messageBytes = Encoding.UTF8.GetBytes(signature);
        using (var hmacsha1 = new HMACSHA1(keyBytes))
        {
            byte[] hashmessage = hmacsha1.ComputeHash(messageBytes);
            return $"AWS {accessKey}:{Convert.ToBase64String(hashmessage)}";
        }
    }
}

public static class HTTPUtils
{
    private static readonly string HttpHost = "10.11.176.109:7000/upload";
    public static UnityWebRequest CreateUploadFileRequest(string filePath, string fileName)
    {
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        form.AddBinaryData("data", fileData);
        UnityWebRequest request = UnityWebRequest.Post(HttpHost + "/" + fileName, form);
        return request;
    }
}