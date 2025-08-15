from pathlib import Path

from back_in_the_game_analyses import Data
from matplotlib import pyplot as plt


def main():
    data_folder = "/home/pariterre/Documents/ShareFolder/MarieLyneNault/BackInTheGame/Nouveau protocole (2025)/"
    subjects = ["orthovr" + str(i) for i in range(1, 16)]

    for subject in subjects:
        print(f"Processing subject: {subject}")
        data_path = Path(data_folder) / subject
        for file in data_path.glob("*.csv"):
            print(f"  Processing file: {file.name}")
            data = Data(file)
            data.head_velocity

            # Plot head position
            plt.figure()
            plt.plot(data.time, data.head_velocity)
            plt.title(f"Head Position Over Time - {subject}")
            plt.xlabel("Time (s)")
            plt.ylabel("Head Position (m)")
            plt.legend(["X", "Y", "Z"])
            plt.show()


if __name__ == "__main__":
    main()
