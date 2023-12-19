"""
Simply display the contents of the webcam with optional mirroring using OpenCV 
via the new Pythonic cv2 interface.  Press <esc> to quit.
"""

import cv2
import subprocess

def show_webcam(mirror=False):

    cam_props = {'brightness': 0, 'contrast': 45, 'saturation': 0,
                'gain': 0, 'gain_automatic': 0, 'white_balance_automatic': 0, 'sharpness': 0, 'auto_exposure': 1,
                'exposure': 255}

### go through and set each property; remember to change your video device if necessary~
### on my RPi, video0 is the usb webcam, but for my laptop the built-in one is 0 and the
### external usb cam is 1
    for key in cam_props:
        subprocess.call(['v4l2-ctl -d /dev/video0 -c {}={}'.format(key, str(cam_props[key]))],
                        shell=True)
    
    ### uncomment to print out/verify the above settings took
    subprocess.call(['v4l2-ctl -d /dev/video0 -l'], shell=True)
    # cam = cv2.VideoCapture(0)
    # #       key value
    # cam.set(3 , 320  ) # width        
    # cam.set(4 , 240  ) # height       
    # # cam.set(10, 0  ) # brightness     min: 0   , max: 255 , increment:1  
    # # cam.set(11, 60   ) # contrast       min: 0   , max: 255 , increment:1     
    # # cam.set(12, 70   ) # saturation     min: 0   , max: 255 , increment:1
    # # cam.set(13, 13   ) # hue         
    # # cam.set(14, 0   ) # gain           min: 0   , max: 127 , increment:1
    # # cam.set(cv2.CAP_PROP_EXPOSURE, -7   ) # exposure       min: -7  , max: -1  , increment:1
    # cam.set(5, 100   ) # FPS  min: 4000, max: 7000, increment:1
    # # cam.set(21, 0   ) # Auto Exposure          min: 0   , max: 255 , increment:5
    # # cam.set(39, -1   ) # Auto Focus          min: 0   , max: 255 , increment:5
    # while True:
    #     ret_val, img = cam.read()
    #     if mirror: 
    #         img = cv2.flip(img, 1)
    #     cv2.imshow('my webcam', img)
    #     if cv2.waitKey(1) == 27: 
    #         break  # esc to quit
    # for i in range(64):
    #     print(f'ID {i} = {cam.get(i)}')
    # cv2.destroyAllWindows()


def main():
    show_webcam(mirror=True)


if __name__ == '__main__':
    main()
