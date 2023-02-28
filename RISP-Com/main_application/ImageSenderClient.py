import socket
import struct
from tkinter import messagebox
from tkinter.filedialog import askopenfilename
import numpy as np
import cv2
import struct
from tkinter import *
np.warnings.filterwarnings('ignore')


class GUIClass(object):
    def __init__(self):
        self.root = Tk()
        self.root.title("RISP Image Sender")
        self.root.geometry("250x80")
        self.client = None
        self.ip_address_lable = Label(self.root, text="IP Address:")
        self.ip_address_lable.grid(row=0, column=0)
        self.ip_address_input = Entry(self.root)
        self.ip_address_input.grid(row=0, column=1)
        self.connect_button = Button(self.root, text="Connect", command=self.connect)
        self.connect_button.grid(row=0, column=2)
        self.connection_status_label = Label(self.root, text="Not Connected")
        self.connection_status_label.grid(row=1, column=1)
        self.send_image_button = Button(self.root, text="Send Image", command=self.send_image)
        self.send_image_button.grid(row=2, column=1)
        self.send_image_button.config(state=DISABLED)
        self.root.protocol("WM_DELETE_WINDOW", self.on_closing)
        self.root.mainloop()

    def connect(self):
        self.ip_address = self.ip_address_input.get()
        self.client = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.client.connect((self.ip_address, 9094))
        self.connection_status_label.config(text="Connected")
        self.connect_button.config(state=DISABLED)
        self.send_image_button.config(state=NORMAL)

    def send_image(self):
        imageFileName = askopenfilename(parent=self.root, title="Choose an image.", filetypes=[("jpeg files", "*.jpg"), ("png files", "*.png")])
        if imageFileName:
            self.image = cv2.imread(imageFileName)
            self.image = cv2.imencode('.jpg', self.image)[1].tobytes()
            try:
                self.client.send(struct.pack('>L', len(self.image)) + self.image)
                messagebox.showinfo("Image sent", "Image sent successfully")
            except:
                messagebox.showerror("Error", "Could not send image")
                self.connection_status_label.config(text="Not Connected")
                self.connect_button.config(state=NORMAL)
                self.send_image_button.config(state=DISABLED)


    def on_closing(self):
        if self.client:
            self.client.close()
        self.root.destroy()

if __name__ == '__main__':
    gui = GUIClass()