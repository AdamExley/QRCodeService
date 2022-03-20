using System;
using Microsoft.MixedReality.Toolkit;
using UnityEngine;

namespace MRTKExtensions.QRCodes
{
    public class QRTrackerController : MonoBehaviour
    {
        [SerializeField]
        private SpatialGraphCoordinateSystemSetter spatialGraphCoordinateSystemSetter;

        [SerializeField]
        private string locationQrValue = string.Empty;

        private TimeSpan lastDetectedSpan; // Add update limits in editor

        private Transform markerHolder;
        private AudioSource audioSource;
        private GameObject markerDisplay;
        private QRInfo lastMessage;
   
        public bool IsTrackingActive { get; private set; } = true;

        private IQRCodeTrackingService qrCodeTrackingService;

        private IQRCodeTrackingService QRCodeTrackingService
        {
            get
            {
                while (!MixedRealityToolkit.IsInitialized && Time.time < 5) ;
                return qrCodeTrackingService ??
                       (qrCodeTrackingService = MixedRealityToolkit.Instance.GetService<IQRCodeTrackingService>());
            }
        }

        private void Start()
        {
            if (!QRCodeTrackingService.IsSupported)
            {
                return;
            }

            markerHolder = spatialGraphCoordinateSystemSetter.gameObject.transform;
            markerDisplay = markerHolder.GetChild(0).gameObject;
            markerDisplay.SetActive(false);

            audioSource = markerHolder.gameObject.GetComponent<AudioSource>();

            QRCodeTrackingService.QRCodeFound += ProcessTrackingFound;
            spatialGraphCoordinateSystemSetter.PositionAcquired += SetPosition;
            spatialGraphCoordinateSystemSetter.PositionAcquisitionFailed +=
                (s, e) => ResetTracking();


            if (QRCodeTrackingService.IsInitialized)
            {
                StartTracking();
            }
            else
            {
                QRCodeTrackingService.Initialized += QRCodeTrackingService_Initialized;
            }
        }

        private void QRCodeTrackingService_Initialized(object sender, EventArgs e)
        {
            StartTracking();
        }

        private void StartTracking()
        {
            QRCodeTrackingService.Enable();
        }

        public void ResetTracking()
        {
            if (QRCodeTrackingService.IsInitialized)
            {
                markerDisplay.SetActive(false);
                IsTrackingActive = true;
            }
        }

        private void ProcessTrackingFound(object sender, QRInfo msg)
        {
            // Would jump on this line
            //if (msg == null || !IsTrackingActive )
            //{
            //    return;
            //}

            lastMessage = msg;

            // Limit pose updating to once every 500 millis
            if (msg.Data == locationQrValue && ((msg.SystemRelativeLastDetectedTime - lastDetectedSpan).TotalMilliseconds > 500))
            {
                lastDetectedSpan = msg.SystemRelativeLastDetectedTime;

                spatialGraphCoordinateSystemSetter.SetLocationIdSize(msg.SpatialGraphNodeId,msg.PhysicalSideLength);
            }
        }

        private void SetPosition(object sender, Pose pose)
        {
            IsTrackingActive = false;
            markerHolder.localScale = Vector3.one * lastMessage.PhysicalSideLength;
            markerDisplay.SetActive(true);
            PositionSet?.Invoke(this, pose);
            audioSource.Play();
        }

        public EventHandler<Pose> PositionSet;
    }
}