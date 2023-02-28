import socket
import struct
import abc
import threading
from datetime import datetime, timedelta
from collections import namedtuple, deque
from enum import Enum
import numpy as np
import cv2
import open3d as o3d
from paint import Paint
from PIL import ImageTk, Image
import struct
import multiprocessing
from queue import Empty as queue_empty
from skimage.viewer import ImageViewer
import time

import ImageSenderClient
np.warnings.filterwarnings('ignore')

IMG_MULTIPLIER = 1.5
TARGET_VOXEL_SIZE = 0.005
DEGUBING = False
# Definitions
# Protocol Header Format
# see https://docs.python.org/2/library/struct.html#format-characters
# VIDEO_STREAM_HEADER_FORMAT = "@qIIII18f"
VIDEO_STREAM_HEADER_FORMAT = "@IIII32f"

system_max = 262144

VIDEO_FRAME_STREAM_HEADER = namedtuple(
    'SensorFrameStreamHeader',
    'ImageWidth ImageHeight PixelStride RowStride '
    'ProjectionM11 ProjectionM12 ProjectionM13 ProjectionM14 '
    'ProjectionM21 ProjectionM22 ProjectionM23 ProjectionM24 '
    'ProjectionM31 ProjectionM32 ProjectionM33 ProjectionM34 '
    'ProjectionM41 ProjectionM42 ProjectionM43 ProjectionM44 '
    'PVtoWorldtransformM11 PVtoWorldtransformM12 PVtoWorldtransformM13 PVtoWorldtransformM14 '
    'PVtoWorldtransformM21 PVtoWorldtransformM22 PVtoWorldtransformM23 PVtoWorldtransformM24 '
    'PVtoWorldtransformM31 PVtoWorldtransformM32 PVtoWorldtransformM33 PVtoWorldtransformM34 '
    'PVtoWorldtransformM41 PVtoWorldtransformM42 PVtoWorldtransformM43 PVtoWorldtransformM44 '
)

VIDEO2_STREAM_HEADER_FORMAT = "@I"
VIDEO2_STREAM_HEADER = namedtuple(
    'VideoFrameStreamHeader',
    'byteLength'
)

# RM_STREAM_HEADER_FORMAT = "@qIIII16f"
RM_STREAM_HEADER_FORMAT = "@IIII"

RM_FRAME_STREAM_HEADER = namedtuple(
    'SensorFrameStreamHeader',
    'ImageWidth ImageHeight PixelStride RowStride '
)

PC_STREAM_HEADER_FORMAT = "@I"

PC_FRAME_STREAM_HEADER = namedtuple(
    'PCFrameStreamHeader',
    'PointCount'
)

# Each port corresponds to a single stream type
VIDEO_STREAM_PORT = 9091
AHAT_STREAM_PORT = 9090
COMMAND_PORT = 9092
VIDEOTWO_STREAM_PORT = 9093

HOST = '192.168.0.41'

HundredsOfNsToMilliseconds = 1e-4
MillisecondsToSeconds = 1e-3


def PixelCoordToWorldCoord(x, y, projectionMatrix, worldMat, image_width, image_height):
    """
    Unproject a point from 2D screen space to 3D world space
    :param x: Screen x coordinate
    :param y: Screen y coordinate
    :param projectionMatrix: Projection matrix
    :return: unprojected point
    """
    halfWidth = image_width / 2.0
    halfHeight = image_height / 2.0
    x = (x - halfWidth) / halfWidth
    y = (y - halfHeight) / halfHeight

    focalPointX = projectionMatrix[0][0]
    focalPointY = projectionMatrix[1][1]
    centerX = projectionMatrix[0][2]
    centerY = projectionMatrix[1][2]

    normFactor = projectionMatrix[2][2]
    centerX = centerX / normFactor
    centerY = centerY / normFactor
    
    dirRayX = (x-centerX) / focalPointX
    dirRayY = (y-centerY) / focalPointY
    dirRayZ = 1.0 / normFactor
    dirRay = np.array([dirRayX, dirRayY, dirRayZ])
    woldDirX = np.dot(worldMat[0][:3], dirRay)
    woldDirY = np.dot(worldMat[1][:3], dirRay)
    woldDirZ = np.dot(worldMat[2][:3], dirRay)
    worldDir = np.array([woldDirX, woldDirY, woldDirZ])
    return worldDir


class SensorType(Enum):
    VIDEO = 1
    AHAT = 2
    LONG_THROW_DEPTH = 3
    LF_VLC = 4
    RF_VLC = 5

def cropCloudPoint(copied_pc, projection_mat, pv_to_world_transform, image_width, image_height):
    leftUpperCorner = PixelCoordToWorldCoord(0,0,projection_mat, pv_to_world_transform, image_width, image_height)
    rightUpperCorner = PixelCoordToWorldCoord(image_width,0,projection_mat, pv_to_world_transform, image_width, image_height)
    rightLowerCorner = PixelCoordToWorldCoord(image_width,image_height,projection_mat, pv_to_world_transform,  image_width, image_height)
    leftLowerCorner = PixelCoordToWorldCoord(0,image_height,projection_mat, pv_to_world_transform, image_width, image_height)
    camera_location = np.array([pv_to_world_transform[0,3], pv_to_world_transform[1,3], pv_to_world_transform[2,3]])
    
    leftLowerCorner = leftLowerCorner / np.linalg.norm(leftLowerCorner)
    leftUpperCorner = leftUpperCorner / np.linalg.norm(leftUpperCorner)
    rightUpperCorner = rightUpperCorner / np.linalg.norm(rightUpperCorner)
    rightLowerCorner = rightLowerCorner / np.linalg.norm(rightLowerCorner)


    leftLowerCorner[2]*= -1
    rightLowerCorner[2]*= -1
    rightUpperCorner[2]*= -1
    leftUpperCorner[2]*= -1
    camera_location[2]*= -1
    lengthForSearch = 2
    points = [leftUpperCorner*lengthForSearch+camera_location, rightUpperCorner*lengthForSearch+camera_location, rightLowerCorner*lengthForSearch+camera_location, leftLowerCorner*lengthForSearch+camera_location, camera_location]
    if DEGUBING:
        lines = [[0,1],[1,2],[2,3],[3,0],[0,4],[1,4],[2,4],[3,4]]
        colors = [[0,0,0],[0,1,0], [0,0,1], [1,1,1]]
        for i in range(len(lines)-4):
            colors.append([1, 0, 0])
        line_set = o3d.geometry.LineSet()
        line_set.points = o3d.utility.Vector3dVector(points)
        line_set.lines = o3d.utility.Vector2iVector(lines)
        line_set.colors = o3d.utility.Vector3dVector(colors)
    searchBoundingBox = o3d.geometry.OrientedBoundingBox.create_from_points(o3d.utility.Vector3dVector(points))
    point_cloud = o3d.geometry.PointCloud()
    
    copied_pc[:, 2] = copied_pc[:, 2] * -1
    point_cloud.points = o3d.utility.Vector3dVector(copied_pc)
    point_cloud = point_cloud.crop(searchBoundingBox)
    point_cloud.estimate_normals()
    if DEGUBING:
        o3d.visualization.draw_geometries([point_cloud, line_set])
    return point_cloud

def reconstructMesh(pointCloud):
    pointCloud.estimate_normals()
    radii = [0.005, 0.01, 0.02, 0.04]
    rec_mesh = o3d.geometry.TriangleMesh.create_from_point_cloud_ball_pivoting(pointCloud, o3d.utility.DoubleVector(radii))
    return rec_mesh

def reconstructMeshMP(points, queue, projection_mat, pv_to_world_transform, image_width, image_height):
    pointCloud = cropCloudPoint(points, projection_mat, pv_to_world_transform, image_width, image_height)
    beforeDownPC = len(pointCloud.points)
    
    pointCloud = pointCloud.voxel_down_sample(voxel_size=TARGET_VOXEL_SIZE)
    afterDownPC = len(pointCloud.points)
    print("Before down: "+str(beforeDownPC)+"; After down: "+str(afterDownPC))
    pointCloud.estimate_normals()
    radious = TARGET_VOXEL_SIZE * 1.3
    radii = [radious, radious*2]
    radii = np.asarray(radii)
    rec_mesh = o3d.geometry.TriangleMesh.create_from_point_cloud_ball_pivoting(pointCloud, o3d.utility.DoubleVector(radii))
    rec_mesh.remove_degenerate_triangles()
    rec_mesh.remove_duplicated_triangles()
    rec_mesh.remove_duplicated_vertices()
    rec_mesh.remove_non_manifold_edges()
    print("Environment reconstruction finished.")
    vertices = np.asarray(rec_mesh.vertices)
    triangles = np.asarray(rec_mesh.triangles)
    queue.put((vertices,triangles))

class ThreadWithReturnValue(threading.Thread):
    def __init__(self, group=None, target=None, name=None, args=(), kwargs={}, Verbose=None):
        threading.Thread.__init__(self, group, target, name, args, kwargs)
        self._return = None

    def run(self):
        if self._target is not None:
            self._return = self._target(*self._args, **self._kwargs)

    def join(self, *args):
        threading.Thread.join(self, *args)
        return self._return

class FrameReceiverThread(threading.Thread):
    def __init__(self, host, port, header_format, header_data):
        super(FrameReceiverThread, self).__init__()
        self.header_size = struct.calcsize(header_format)
        self.header_format = header_format
        self.header_data = header_data
        self.host = host
        self.port = port
        self.latest_frame = None
        self.latest_header = None
        self.socket = None

    def get_data_from_socket(self):
        # read the header in chunks
        reply = self.recvall(self.header_size)

        if not reply:
            print('ERROR: Failed to receive data from stream.')
            return

        data = struct.unpack(self.header_format, reply)
        header = self.header_data(*data)

        # read the image in chunks
        image_size_bytes = header.ImageHeight * header.RowStride
        image_data = bytes()
        if image_size_bytes > system_max:
            cut  = int(image_size_bytes / system_max)
            leftOver = image_size_bytes % system_max

            for index in range (0, cut):
                image_data += self.recvall(system_max)
            if leftOver > 0:
                image_data += self.recvall(leftOver)
        else:
            image_data += self.recvall(image_size_bytes)
        return header, image_data

    def recvall(self, size):
        msg = bytes()
        while len(msg) < size:
            part = self.socket.recv(size - len(msg))
            if part == '':
                break  # the connection is closed
            msg += part
        return msg

    def start_socket(self):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))
        print('INFO: Socket connected to ' + self.host + ' on port ' + str(self.port))

    def start_listen(self):
        t = threading.Thread(target=self.listen)
        t.start()

    @abc.abstractmethod
    def listen(self):
        return

    @abc.abstractmethod
    def get_mat_from_header(self, header):
        return

class PCReceiverThread(threading.Thread):
    def __init__(self, host, port, header_format, header_data):
        super(PCReceiverThread, self).__init__()
        self.header_size = struct.calcsize(header_format)
        self.header_format = header_format
        self.header_data = header_data
        self.host = host
        self.port = port
        self.latest_pc = None
        self.latest_header = None
        self.socket = None

    def get_data_from_socket(self):
        # read the header in chunks
        reply = self.recvall(self.header_size)

        if not reply:
            print('ERROR: Failed to receive data from stream.')
            return

        data = struct.unpack(self.header_format, reply)
        header = self.header_data(*data)

        # read the image in chunks
        array_length = header.PointCount
        array_size_bytes = array_length * 4
        array_data = bytes()
        
        if array_size_bytes > system_max:
            cut  = int(array_size_bytes / system_max)
            leftOver = array_size_bytes % system_max

            for index in range (0, cut):
                array_data += self.recvall(system_max)
            if leftOver > 0:
                array_data += self.recvall(leftOver)
        else:
            array_data += self.recvall(array_data)
        return header, array_data

    def recvall(self, size):
        msg = bytes()
        while len(msg) < size:
            part = self.socket.recv(size - len(msg))
            if part == '':
                break  # the connection is closed
            msg += part
        return msg

    def start_socket(self):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))
        # send_message(self.socket, b'socket connected at ')
        print('INFO: Socket connected to ' + self.host + ' on port ' + str(self.port))

    def start_listen(self):
        t = threading.Thread(target=self.listen)
        t.start()

    @abc.abstractmethod
    def listen(self):
        return


class CommandThread(threading.Thread):
    def __init__(self, host, port, header_format, header_data):
        super(CommandThread, self).__init__()
        self.header_size = struct.calcsize(header_format)
        self.header_format = header_format
        self.header_data = header_data
        self.host = host
        self.port = port
        self.latest_pc = None
        self.latest_header = None
        self.socket = None

    def get_data_from_socket(self):
        # read the header in chunks
        reply = self.recvall(self.header_size)

        if not reply:
            print('ERROR: Failed to receive data from stream.')
            return

        data = struct.unpack(self.header_format, reply)
        header = self.header_data(*data)

        # read the image in chunks
        array_length = header.PointCount
        array_size_bytes = array_length * 4
        array_data = bytes()
        
        if array_size_bytes > system_max:
            cut  = int(array_size_bytes / system_max)
            leftOver = array_size_bytes % system_max

            for index in range (0, cut):
                array_data += self.recvall(system_max)
            if leftOver > 0:
                array_data += self.recvall(leftOver)
        else:
            array_data += self.recvall(array_data)
        return header, array_data

    def recvall(self, size):
        msg = bytes()
        while len(msg) < size:
            part = self.socket.recv(size - len(msg))
            if part == '':
                break  # the connection is closed
            msg += part
        return msg

    def start_socket(self):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))
        # send_message(self.socket, b'socket connected at ')
        print('INFO: Socket connected to ' + self.host + ' on port ' + str(self.port))

    def start_listen(self):
        t = threading.Thread(target=self.listen)
        t.start()

    @abc.abstractmethod
    def listen(self):
        return

class VideoTwoReceiverThread(threading.Thread):
    def __init__(self, host, port, header_format, header_data):
        super(VideoTwoReceiverThread, self).__init__()
        self.header_size = struct.calcsize(header_format)
        self.header_format = header_format
        self.header_data = header_data
        self.host = host
        self.port = port
        self.latest_frame = None
        self.latest_header = None
        self.socket = None
        self.gui = None

    def get_data_from_socket(self):
        # read the header in chunks
        reply = self.recvall(self.header_size)

        if not reply:
            print('ERROR: Failed to receive data from stream.')
            return

        data = struct.unpack(self.header_format, reply)
        header = self.header_data(*data)

        # read the image in chunks
        image_size_bytes = header.byteLength
        image_data = bytes()
        if image_size_bytes > system_max:
            cut  = int(image_size_bytes / system_max)
            leftOver = image_size_bytes % system_max

            for index in range (0, cut):
                image_data += self.recvall(system_max)
            if leftOver > 0:
                image_data += self.recvall(leftOver)
        else:
            image_data += self.recvall(image_size_bytes)
        return header, image_data

    def recvall(self, size):
        msg = bytes()
        while len(msg) < size:
            part = self.socket.recv(size - len(msg))
            if part == '':
                break  # the connection is closed
            msg += part
        return msg

    def start_socket(self):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))
        # send_message(self.socket, b'socket connected at ')
        print('INFO: Socket connected to ' + self.host + ' on port ' + str(self.port))

    def start_listen(self):
        t = threading.Thread(target=self.listen)
        t.start()

    @abc.abstractmethod
    def listen(self):
        return

    @abc.abstractmethod
    def get_mat_from_header(self, header):
        return

class MeshReconstruction(threading.Thread):
    def __init__(self):
        super().__init__()
        self.pointCloud = None
        self.reconstructedMesh = None
    def run(self):
        self.reconstructedMesh = reconstructMesh(self.pointCloud)

class VideoReceiverThread(FrameReceiverThread):
    def __init__(self, host):
        super().__init__(host, VIDEO_STREAM_PORT, VIDEO_STREAM_HEADER_FORMAT,
                         VIDEO_FRAME_STREAM_HEADER)

    def listen(self):
        while True:
            self.latest_header, image_data = self.get_data_from_socket()
            self.latest_frame = np.frombuffer(image_data, dtype=np.uint8).reshape((self.latest_header.ImageHeight,
                                                                                   self.latest_header.ImageWidth,
                                                                                   self.latest_header.PixelStride))
            print("Received frame")
    def get_mat_from_header(self, header):
        projection_mat = np.array(header[4:20]).reshape((4,4))
        pv_to_world_transform = np.array(header[20:36]).reshape((4, 4))
        return projection_mat, pv_to_world_transform


class AhatReceiverThread(FrameReceiverThread):
    def __init__(self, host):
        super().__init__(host,
                         AHAT_STREAM_PORT, RM_STREAM_HEADER_FORMAT, RM_FRAME_STREAM_HEADER)

    def listen(self):
        while True:
            self.latest_header, image_data = self.get_data_from_socket()
            self.latest_frame = np.frombuffer(image_data, dtype=np.uint16).reshape((self.latest_header.ImageHeight,
                                                                                    self.latest_header.ImageWidth))
            print("Received frame")
    def get_mat_from_header(self, header):
        rig_to_world_transform = np.array(header[5:22]).reshape((4, 4)).T
        return rig_to_world_transform


class PC_ReceiverThread(PCReceiverThread):
    def __init__(self, host):
        super().__init__(host, AHAT_STREAM_PORT, PC_STREAM_HEADER_FORMAT, PC_FRAME_STREAM_HEADER)

    def listen(self):
        while True:
            self.latest_header, array_data = self.get_data_from_socket()
            self.latest_pc = np.frombuffer(array_data, dtype=np.float32).reshape((int(self.latest_header.PointCount/3), 3))
            print("PC_ReceiverThread: Received point cloud with " + str(self.latest_pc.shape[0]) + " points.")

class Command_Thread(CommandThread):
    def __init__(self, host):
        super().__init__(host, COMMAND_PORT, PC_STREAM_HEADER_FORMAT, PC_FRAME_STREAM_HEADER)

    def listen(self):
        while True:
            self.latest_header, array_data = self.get_data_from_socket()
            self.latest_pc = np.frombuffer(array_data, dtype=np.float32).reshape((int(self.latest_header.PointCount/3), 3))
            print("PC_ReceiverThread: Received point cloud with " + str(self.latest_pc.shape[0]) + " points.")

    def sendPoint(self, pointList):
        listLength = len(pointList) * 3
        val = struct.pack('!i', listLength)
        for element in pointList:
            for element2 in element:
                val+= struct.pack("!f", element2)
        self.socket.send(val)
    def sendLines(self, lines):
        line_keys = list(lines.keys())
        # packet payload order number of lines, line color in int, number of vertices, vertices
        numOfLines = len(line_keys)
        val = struct.pack('!i', numOfLines)
        for key in line_keys:
            ColorHex = key.replace('#','')
            ColorInt = int(ColorHex,16)
            val += struct.pack('!i', ColorInt)
            pointList = lines[key]
            listLength = len(pointList) * 3
            val += struct.pack('!i', listLength)
            for element in pointList:
                for element2 in element:
                    val+= struct.pack("!f", element2)
        self.socket.send(val)

class VideoTwoReceiverThreadFront(VideoTwoReceiverThread):
    def __init__(self, host):
        super().__init__(host, VIDEOTWO_STREAM_PORT, VIDEO2_STREAM_HEADER_FORMAT,
                         VIDEO2_STREAM_HEADER)
    def listen(self):
        while True:
            self.latest_header, image_data = self.get_data_from_socket()
            encoded_img = np.fromstring(image_data, dtype = np.uint8)
            # self.latest_frame = np.asarray(np.frombuffer(image_data, dtype=np.uint8), dtype=np.uint8)
            image = cv2.imdecode(encoded_img, cv2.IMREAD_COLOR)
            image = cv2.flip(image, 0)
            image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
            if IMG_MULTIPLIER != 1:
                height, width, _ = image.shape
                newHeight = int(height *IMG_MULTIPLIER)
                newWidth = int(width * IMG_MULTIPLIER)
                image = cv2.resize(image, (newWidth, newHeight))

            cv2.imshow("Video", image)
            if cv2.waitKey(1) == ord('q'):
                break


def run():
    print("Loading...")
    multiprocessing.freeze_support()
    HOST = input("Enter HoloLens IP address: ")
    video_receiver = VideoReceiverThread(HOST)
    video_receiver.start_socket()



    video_receiver.start_listen()
    pc_receiver = PC_ReceiverThread(HOST)
    pc_receiver.start_socket()
    pc_receiver.start_listen()

    command = Command_Thread(HOST)
    command.start_socket()

    videotwo_receiver = VideoTwoReceiverThreadFront(HOST)
    videotwo_receiver.start_socket()
    videotwo_receiver.start_listen()

    while True:
        if np.any(video_receiver.latest_frame) and np.any(pc_receiver.latest_pc):
            image_array = np.copy(video_receiver.latest_frame)
            copied_pc = np.copy(pc_receiver.latest_pc)
            projection_mat, pv_to_world_transform = video_receiver.get_mat_from_header(video_receiver.latest_header)
            camera_location = np.array([pv_to_world_transform[0,3], pv_to_world_transform[1,3], pv_to_world_transform[2,3]])
            camera_location[2]*= -1
            image_width = video_receiver.latest_header.ImageWidth
            image_height = video_receiver.latest_header.ImageHeight

            mpQueue = multiprocessing.Queue()
            meshProcess = multiprocessing.Process(target=reconstructMeshMP, args=(copied_pc,mpQueue,projection_mat, pv_to_world_transform, image_width, image_height))
            meshProcess.daemon = True
            meshProcess.start()
            video_receiver.latest_frame = None
            pc_receiver.latest_pc = None
            if DEGUBING:
                print(projection_mat)
                print(pv_to_world_transform)

            converted_image = cv2.cvtColor(image_array, cv2.COLOR_BGRA2RGB)
            #Image resizer
            new_img_width = int(image_width * IMG_MULTIPLIER)
            new_img_height = int(image_height * IMG_MULTIPLIER)           
            img = Image.fromarray(converted_image)
            paint = Paint(new_img_width, new_img_height, new_img_width/float(image_width), new_img_height/float(image_height))
            resized_img = img.copy().resize((new_img_width,new_img_height), Image.ANTIALIAS)
            resized_img = ImageTk.PhotoImage(resized_img)
            paint.setImage(resized_img)
            paint.run()


            draw_array = paint.getPaintArray()
            if len(draw_array) < 1:
                continue
            # print(draw_array)
            draw_array_keys = list(draw_array.keys())
            # meshProcess.join()
            print("Got "+str(len(draw_array_keys))+" annotation(s).")
            print("Waiting for surface reconstruction...")
            verticesNTriangles = mpQueue.get()
            meshProcess.terminate()
            print("Reconstruction finished.")
            print("Finding annotation location in real world...")
            rec_mesh = o3d.geometry.TriangleMesh()
            rec_mesh.vertices = o3d.utility.Vector3dVector(verticesNTriangles[0])
            rec_mesh.triangles = o3d.utility.Vector3iVector(verticesNTriangles[1])
            scene = o3d.t.geometry.RaycastingScene()
            rec_mesh_leg = o3d.t.geometry.TriangleMesh.from_legacy(rec_mesh)
            scene.add_triangles(rec_mesh_leg)
            linesInWorldPos = {}
            for draw_array_key in draw_array_keys:
                vertices = draw_array[draw_array_key]
                converted_vertices = []
                for vertex in vertices:
                    converted_vertices.append((vertex[0], image_height - vertex[1]))
                if DEGUBING:
                    print(converted_vertices)
                query_points = []
                worldDirs = []
                for element in converted_vertices:
                    worldDir = PixelCoordToWorldCoord(element[0], element[1], projection_mat, pv_to_world_transform, image_width, image_height)
                    worldDir[2]*= -1
                    worldDirs.append(worldDir)
                    query_points.append([camera_location[0], camera_location[1], camera_location[2], worldDir[0], worldDir[1], worldDir[2]])
                query_points = o3d.core.Tensor(np.array(query_points).astype(np.float32))
                hit_dict = scene.cast_rays(query_points)
                if DEGUBING:
                    print(hit_dict)
                t_hits = hit_dict['t_hit'].numpy()

                final_points = []
                for index in range(len(t_hits)):
                    if t_hits[index] < 3:
                        normalized =  worldDirs[index] 
                        point  = camera_location + normalized * float(t_hits[index])
                        final_points.append(point)
                if DEGUBING:
                    print(final_points)
                    final_lines = []
                    for index in range(len(final_points)-1):
                        final_lines.append([index, index+1])
                    final_colors = [[1,0,0] for i in range(len(final_lines))]
                    line_set = o3d.geometry.LineSet()
                    line_set.points = o3d.utility.Vector3dVector(final_points)
                    line_set.lines = o3d.utility.Vector2iVector(final_lines)
                    line_set.colors = o3d.utility.Vector3dVector(final_colors)
                    rec_mesh.compute_vertex_normals()
                    rec_mesh.paint_uniform_color([1, 0.706, 0])
                    o3d.visualization.draw_geometries([rec_mesh], mesh_show_back_face=True)
                    o3d.visualization.draw_geometries([rec_mesh, line_set], mesh_show_back_face=True)
                final_points = np.array(final_points)
                final_points[:,2] *= -1
                linesInWorldPos[draw_array_key] = final_points
                if DEGUBING:
                    print(final_points)
            print("Localized real world location(s).")
            command.sendLines(linesInWorldPos)
            print("Annotation(s) was sent to HoloLens.")
            if DEGUBING:
                input()
        time.sleep(0.5)

def StartImageSenderClient():
    global client
    client = ImageSenderClient.GUIClass()

if __name__ == '__main__':
    p = multiprocessing.Process(target=StartImageSenderClient)
    p.daemon = True
    p.start()
    run()


