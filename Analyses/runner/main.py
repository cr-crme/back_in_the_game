from pathlib import Path

from back_in_the_game_analyses import Data, DataTag, DataType, DataAxis
from matplotlib import pyplot as plt


# Y-Balance Test
# 3. Oscillation des mains => Pic accélération, distance totale parcourue

# Single Hop test
# 4. Temps de vol => Position tête neutre + vitesse vertical (+ début, - fin)

# Single Leg Vertical Jump
# 4. Distance horizontale => 1 seconde début (mean), 1 seconde fin (mean)


def main():
    data_folder = "/home/pariterre/Documents/ShareFolder/MarieLyneNault/BackInTheGame/Nouveau protocole (2025)/"
    subjects = ["orthovr" + str(i) for i in range(1, 16)]

    for subject in subjects:
        print(f"Processing subject: {subject}")
        data_path = Path(data_folder) / subject
        for file in data_path.glob("*.csv"):
            file = Path(f"{data_folder}orthovr1/58_JumpSingleGL_Soccer.csv.csv")
            print(f"  Processing file: {file.name}")
            data = Data(file)

            # Compute metrics
            head_horizontal_dispersion = data.horizontal_dispersion(DataTag.HEAD_POSITION)
            squat_height = data.displacement(DataTag.HEAD_POSITION, axis=DataAxis.VERTICAL)
            left_hand_acceleration_peak = data.acceleration_peak(DataTag.LEFT_HAND_POSITION)
            right_hand_acceleration_peak = data.acceleration_peak(DataTag.RIGHT_HAND_POSITION)
            left_hand_traveled_distance = data.traveled_distance(DataTag.LEFT_HAND_POSITION)
            right_hand_traveled_distance = data.traveled_distance(DataTag.RIGHT_HAND_POSITION)

            # Jump specific metrics
            # Strategy:
            #   1) Find the first time the velocity is more negative that a threshold (-5m/s) (mid_squat_descent_idx)
            #   2) Walk forwards to find the first time the velocity is more positive that the threshold (-5m/s) (mid_squat_ascent_idx)
            #   3) Walk backwards to find the last time the velocity was zero (start_squat_idx)
            #   4) Between mid_squat_descent_idx and mid_squat_ascent_idx, find the lowest point of the head position (this is the deepest_squat_index)
            #   5) Walk forwards for the first time the acceleration crosses zero (this is the toe-off index)
            #   6) Walk forwards to find the highest point of the head position (this is the highest point index)
            #   7) Walk forwards for the first time the acceleration crosses zero again (this is the reception index)

            # Get useful aliases
            velocity_threshold = -5.0  # m/s
            head_pos = data.get(DataTag.HEAD_POSITION, data_type=DataType.VALUE, axis=DataAxis.VERTICAL)
            head_vel = data.get(DataTag.HEAD_POSITION, data_type=DataType.VELOCITY, axis=DataAxis.VERTICAL)
            head_acc = data.get(DataTag.HEAD_POSITION, data_type=DataType.ACCELERATION, axis=DataAxis.VERTICAL)

            mid_squat_descent_idx = (head_vel < velocity_threshold).idxmax().iloc[0]
            mid_squat_ascent_idx = (head_vel.loc[mid_squat_descent_idx:] > velocity_threshold).idxmax().iloc[0]

            start_squat_idx = (head_vel.loc[:mid_squat_descent_idx].iloc[::-1] > 0).idxmax()

            start_squat_idx = (
                data.get(DataTag.HEAD_POSITION, data_type=DataType.VELOCITY, axis=DataAxis.VERTICAL).iloc[
                    :mid_squat_decent_idx
                ]
                > 0
            )

            # deep_squat_idx = data.get(DataTag.HEAD_POSITION, axis=DataAxis.VERTICAL).idxmin().iloc[0]
            # toe_off_idx = data.get(DataTag.HEAD_POSITION, axis=DataAxis.VERTICAL).idxmin().iloc[0]
            # highest_point_idx = data.get(DataTag.HEAD_POSITION, axis=DataAxis.VERTICAL).idxmax().iloc[0]
            # pre_jump_slice = slice(pre_jump_end_idx)
            # post_jump_slice = slice(pre_jump_end_idx, data.shape[0])

            jump_distance = data.displacement(DataTag.HEAD_POSITION, axis=DataAxis.FRONTAL)
            jump_height = squat_height

            # Plot head position
            plt.figure(file.name)
            plt.title(f"{subject} - {file.stem}")
            plt.subplot(3, 1, 1)
            plt.plot(data.time, data.get(DataTag.HEAD_POSITION, data_type=DataType.VALUE, axis=DataAxis.Y))
            plt.ylabel("Position horizontal (m)")
            plt.subplot(3, 1, 2)
            plt.plot(data.time, data.get(DataTag.HEAD_POSITION, data_type=DataType.VELOCITY, axis=DataAxis.Y))
            plt.ylabel("Head Velocity (m)")
            plt.subplot(3, 1, 3)
            plt.plot(data.time, data.get(DataTag.HEAD_POSITION, data_type=DataType.ACCELERATION, axis=DataAxis.Y))
            plt.ylabel("Head Acceleration (m/s²)")
            plt.xlabel("Time (s)")
            plt.tight_layout()
            plt.show()


if __name__ == "__main__":
    main()
