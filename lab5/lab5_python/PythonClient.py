import threading
import time
from Message import *

def process_messages():
    """Background thread: poll server for incoming messages."""
    while True:
        try:
            m = Message.send_message(MR_BROKER, MT_GETDATA)
            if m.header.type == MT_DATA:
                print("[Client {}]: {}".format(m.header.from_id, m.data))
                print("> ", end="", flush=True)
        except Exception:
            print("Server disconnected.")
            break
        time.sleep(0.1)

def main():
    print("Connecting to server...")
    try:
        Message.send_message(MR_BROKER, MT_INIT)
    except Exception as e:
        print("Cannot connect to server: {}".format(e))
        return

    print("Connected! Client ID: {}".format(Message.ClientID))
    print("Type messages and press Enter to send. Type 'exit' to quit.")

    t = threading.Thread(target=process_messages, daemon=True)
    t.start()

    while True:
        try:
            text = input("> ")
            if text.lower() == 'exit':
                try:
                    Message.send_message(MR_BROKER, MT_EXIT)
                except:
                    pass
                print("Disconnected.")
                break
            if text:
                Message.send_message(MR_ALL, MT_DATA, text)
        except (EOFError, KeyboardInterrupt):
            try:
                Message.send_message(MR_BROKER, MT_EXIT)
            except:
                pass
            print("Disconnected.")
            break

if __name__ == "__main__":
    main()