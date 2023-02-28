from tkinter import *
from tkinter.colorchooser import askcolor
from PIL import ImageTk, Image

class Paint(object):

    DEFAULT_PEN_SIZE = 2
    DEFAULT_COLOR = '#ff0000'


    def __init__(self, canvas_width, canvas_height, width_multiplier, height_multiplier):
        self.root = Tk()
        self.width_multiplier = 1/width_multiplier
        self.height_multiplier = 1/height_multiplier

        self.pen_button = Button(self.root, text='pen', command=self.use_pen)
        self.pen_button.config(relief=SUNKEN)
        self.pen_button.grid(row=0, column=0)

        self.line_button = Button(self.root, text='line', command=self.use_line)
        self.line_button.grid(row=0, column=1)

        # self.brush_button = Button(self.root, text='brush', command=self.use_brush)
        # self.brush_button.grid(row=0, column=1)

        self.color_button = Button(self.root, text='color', command=self.choose_color)
        self.color_button.grid(row=0, column=2)

        # self.eraser_button = Button(self.root, text='eraser', command=self.use_eraser)
        # self.eraser_button.grid(row=0, column=3)
        
        self.reset_button = Button(self.root, text='reset', command=self.reset_btn)
        self.reset_button.grid(row=0, column=3)

        self.choose_size_button = Scale(self.root, from_=1, to=10, orient=HORIZONTAL, label="Thickness")
        self.choose_size_button.set(self.DEFAULT_PEN_SIZE)
        self.choose_size_button.grid(row=0, column=4)

        self.confirm_button = Button(self.root, text='confirm', command=self.confirm)
        self.confirm_button.grid(row=0, column=5)

        self.c = Canvas(self.root, bg='white', width=canvas_width, height=canvas_height)
        self.c.grid(row=1, columnspan=6)
        self.root.focus_set()

        self.setup()
    
    def run(self):
        self.root.mainloop()

    def setup(self):
        self.old_x = None
        self.old_y = None
        self.line_width = self.choose_size_button.get()
        self.color = self.DEFAULT_COLOR
        self.line_start = False
        self.eraser_on = False
        self.active_button = self.pen_button
        self.c.bind('<B1-Motion>', self.paint)
        self.c.bind('<ButtonRelease-1>', self.reset)
        self.paint_array = {}
        self.line_start_x = None
        self.line_start_y = None
        self.line_end_x = None
        self.line_end_y = None
        self.first_click = True

    def use_pen(self):
        self.activate_button(self.pen_button)

    # def use_brush(self):
    #     self.activate_button(self.brush_button)

    def choose_color(self):
        self.eraser_on = False
        self.color = askcolor(color=self.color)[1]

    def use_eraser(self):
        self.activate_button(self.eraser_button, eraser_mode=True)

    def activate_button(self, some_button, eraser_mode=False):
        self.active_button.config(relief=RAISED)
        some_button.config(relief=SUNKEN)
        self.active_button = some_button
        self.eraser_on = eraser_mode

    def paint(self, event):
        self.line_width = self.choose_size_button.get()
        paint_color = 'white' if self.eraser_on else self.color
        if self.first_click:
            self.first_click = False
            self.paint_array[self.color] = []
            if self.paint_array.get(self.color) is not None:
                self.c.delete("line"+str(self.color))
                self.c.delete("DrewLine"+str(self.color))

        if self.active_button == self.pen_button:
            if self.old_x and self.old_y:
                self.c.create_line(self.old_x, self.old_y, event.x, event.y,
                                width=self.line_width, fill=paint_color,
                                capstyle=ROUND, smooth=TRUE, splinesteps=36, tags="line"+str(self.color))
            self.old_x = event.x
            self.old_y = event.y
            if event.x > -1 and event.y > -1:
                # if self.paint_array.get(paint_color) is None:
                #     self.paint_array[paint_color] = []
                self.paint_array[paint_color].append((event.x * self.width_multiplier, event.y * self.height_multiplier))
                # self.paint_array.append([event.x, event.y, paint_color])
        if self.active_button == self.line_button:
            if not self.line_start:
                self.line_start = True
                self.old_x = event.x
                self.old_y = event.y
                self.line_start_x = self.old_x
                self.line_start_y = self.old_y
            else:
                if self.old_x != event.x or self.old_y != event.y:
                    self.c.delete("DrawingLine")
                    self.c.create_line(self.line_start_x,self.line_start_y, event.x, event.y,
                                    width=self.line_width, fill=paint_color,
                                    capstyle=ROUND, smooth=TRUE, splinesteps=36, tags="DrawingLine")
                    self.old_x = event.x
                    self.old_y = event.y
    def use_line(self):
         self.activate_button(self.line_button)

    def reset(self, event):
        self.old_x, self.old_y = None, None
        self.first_click = True
        if self.active_button == self.line_button and self.line_start:
            self.line_start = False
            self.line_end_x = event.x
            self.line_end_y = event.y
            self.c.delete("DrawingLine")
            self.c.create_line(self.line_start_x,self.line_start_y, event.x, event.y,
                            width=self.line_width, fill=self.color,
                            capstyle=ROUND, smooth=TRUE, splinesteps=36, tags="DrewLine"+str(self.color))
            points_on_line = self.get_line(self.line_start_x, self.line_start_y, self.line_end_x, self.line_end_y)
            for point in points_on_line:
                self.paint_array[self.color].append((int(point[0] * self.width_multiplier), int(point[1] * self.height_multiplier)))
            # self.paint_array[self.color].append((self.line_start_x, self.line_start_y))
            # self.paint_array[self.color].append((self.line_end_x, self.line_end_y))
    
    def reset_btn(self):
        self.c.delete("line")
        self.paint_array = []
    
    def setImage(self, image):
        self.c.create_image(0, 0, image=image, anchor=NW)
    
    def confirm(self):
        self.root.destroy()
    
    def getPaintArray(self):
        return self.paint_array
    
    def get_line(self, x1, y1, x2, y2):
        points = []
        issteep = abs(y2-y1) > abs(x2-x1)
        if issteep:
            x1, y1 = y1, x1
            x2, y2 = y2, x2
        rev = False
        if x1 > x2:
            x1, x2 = x2, x1
            y1, y2 = y2, y1
            rev = True
        deltax = x2 - x1
        deltay = abs(y2-y1)
        error = int(deltax / 2)
        y = y1
        ystep = None
        if y1 < y2:
            ystep = 1
        else:
            ystep = -1
        for x in range(x1, x2 + 1):
            if issteep:
                points.append((y, x))
            else:
                points.append((x, y))
            error -= deltay
            if error < 0:
                y += ystep
                error += deltax
        # Reverse the list if the coordinates were reversed
        if rev:
            points.reverse()
        return points

if __name__ == '__main__':
    paint = Paint()
    img = ImageTk.PhotoImage(file=r'E:\\github\\RISP\\python\\images\\test.jpg')
    paint.setImage(img)
    paint.run()
    line_array = paint.getPaintArray()
    print(line_array)