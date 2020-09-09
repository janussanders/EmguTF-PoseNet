﻿using System;
using System.Drawing;
using System.Windows.Forms;
using Emgu.CV; //VideoCapture, Mat 
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;


namespace EmguTF_pose
{ 

    public partial class EmguTF_posewindow : Form
    {
        /// <summary>
        /// Our video m_Webcam object. Abstraction for our m_Webcam.
        /// </summary>
        private VideoCapture m_Webcam;

        /// <summary>
        /// Our current m_frame to process.
        /// </summary>
        private Mat m_frame;

        /// <summary>
        /// Our pose estimator
        /// </summary>
        private PoseNetEstimator m_posenet;

        /// <summary>
        /// A basic flag checking if process frame is ongoing or not.
        /// </summary>
        static bool inprocessframe = false;

        /// <summary>
        /// A basic flag checking if process is ongoing or not
        /// </summary>
        static bool inprocess = false;

        /// <summary>
        /// __init__
        /// </summary>
        public EmguTF_posewindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Everything start and stop on a button click.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void button_start_Click(object sender, EventArgs e)
        {
            if (button_start.Text != "Stop")
            {
                button_start.Text = "Stop";
                m_Webcam.Start();
            }
            else
            {
                m_Webcam.Stop();
                button_start.Text = "Start";
            }
        }

        /// <summary>
        /// Called on load event. It will instantiate this class variables, including
        /// the callbacks functions.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EmguTF_posewindow_Load(object sender, EventArgs e)
        {
            // Main Emgu CV elements to work with m_Webcam
            // 1- An matrix / image to store the last grabbed frame
            m_frame = new Mat();

            // 2- Our webcam will represent the first camera found on our device
            //m_Webcam = new VideoCapture(0);
            m_Webcam = new VideoCapture("http://root:onioneer@192.168.3.1:8080/?action=stream",VideoCapture.API.Any);

            // 3- When the webcam capture (grab) an image, callback on ProcessFrame method
            m_Webcam.ImageGrabbed += new EventHandler(Process); // event based

            string model = Application.StartupPath + "\\..\\..\\..\\..\\models\\posenet_mobilenet_v1_100_257x257_multi_kpt_stripped.tflite";
            // Pose estimator - to do remove hardcoded things !
            m_posenet = new PoseNetEstimator(frozenModelPath: model, numberOfThreads: 4);

        }

        /// <summary>
        /// Handle the image grabbed event by retriving the frame, processing it and showing it.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Process(object sender, EventArgs e)
        {
            if (!inprocess)
            {
                // Say we start to process
                inprocess = true;

                // Reinit m_frame
                m_frame = new Mat();

                // Retrieve
                m_Webcam.Retrieve(m_frame);

                // If frame is not empty, try to process it
                if (!m_frame.IsEmpty)
                {
                    //if not already processing previous frame, process it
                    if (!inprocessframe)
                    {
                        ProcessFrame(m_frame.Clone());
                    }

                    // Display keypoints and frame in imageview
                    ShowKeypoints();
                    ShowJoints();
                    ShowFrame();

                }

                // Say we finished to process 
                m_frame.Dispose();
                inprocess = false;
            }
        }

        /// <summary>
        /// A method to get keypoints from a frame.
        /// </summary>
        /// <param name="frame">A copy of <see cref="m_frame"/>. It could be resized beforehand.</param>
        public void ProcessFrame(Emgu.CV.Mat frame)
        {
            if (!inprocessframe)
            {
                inprocessframe = true;
                DateTime start = DateTime.Now;

                m_posenet.Inference(frame);

                DateTime stop = DateTime.Now;
                long elapsedTicks = stop.Ticks - start.Ticks;
                TimeSpan elapsedSpan = new TimeSpan(elapsedTicks);
                Console.WriteLine(1000/(double)elapsedSpan.Milliseconds);

                inprocessframe = false;
            }
        }

        /// <summary>
        /// Show<see cref="m_posenet.m_keypoints"/> on <see cref="m_frame"/> if keypoint is not Point(-1,-1).
        /// </summary>
        private void ShowKeypoints()
        {
            if (!m_frame.IsEmpty)
            {
                float count = 1;
                foreach (Keypoint kpt in m_posenet.m_keypoints) // if not empty array of points
                {
                    if (kpt != null)
                    {
                        if ((kpt.position.X != -1) & (kpt.position.Y != -1)) // if points are valids
                        {
                            Emgu.CV.CvInvoke.Circle(m_frame, kpt.position,
                                                    3, new MCvScalar(200, 255, (int)((float)255 / count), 255), 2);
                        }
                        count++;
                    }
                }
            }
        }


        /// <summary>
        /// Show<see cref="m_posenet.m_keypointsJoints"/> on <see cref="m_frame"/> if none of the 2 keypoints linked by
        /// a joint is equal to Point(-1,-1).
        /// </summary>
        private void ShowJoints()
        {
            if (!m_frame.IsEmpty)
            {
                foreach (int[] joint in m_posenet.m_keypointsJoints) // if not empty array of points
                {
                    if (m_posenet.m_keypoints[joint[0]] != null & m_posenet.m_keypoints[joint[1]] != null)
                    {
                        if ((m_posenet.m_keypoints[joint[0]].position != new Point(-1, -1)) &
                            (m_posenet.m_keypoints[joint[0]].position != new Point(0, 0)) &
                            (m_posenet.m_keypoints[joint[1]].position != new Point(-1, -1))  &
                            (m_posenet.m_keypoints[joint[1]].position != new Point(0, 0))) // if points are valids
                        {
                            Emgu.CV.CvInvoke.Line(m_frame,
                                                  m_posenet.m_keypoints[joint[0]].position,
                                                  m_posenet.m_keypoints[joint[1]].position,
                                                  new MCvScalar(200, 255, 255, 255), 2);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Show <see cref="m_frame"/> on an ImageBox.
        /// </summary>
        private void ShowFrame()
        {
            if (!m_frame.IsEmpty)
            {
                CvInvoke.Resize(m_frame, m_frame, new Size(imageBox.Size.Width, imageBox.Size.Height));
                Emgu.CV.CvInvoke.Flip(m_frame, m_frame, FlipType.Horizontal);
                imageBox.Image = m_frame;
                Emgu.CV.CvInvoke.WaitKey(1); //wait a few clock cycles
            }
        }
    }
}
