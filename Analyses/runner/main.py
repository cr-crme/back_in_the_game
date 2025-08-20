import os
from pathlib import Path

from back_in_the_game_analyses import (
    Data,
    DataTag,
    DataType,
    DataAxis,
    data_extraction,
    DataIndicesExtraction,
    DataMetrics,
)
from matplotlib import pyplot as plt
import pandas as pd

_show_graphs = False


def main():
    # Get the data folder from the DATA_PATH environment variable
    data_folder = os.getenv("DATA_PATH")
    subjects = ["orthovr" + str(i) for i in range(1, 16)]
    all_metrics = []

    for subject in subjects:
        print(f"Processing subject: {subject}")
        data_path = Path(data_folder) / subject
        for file in data_path.glob("*.csv"):
            print(f"  Processing file: {file.name}")
            data = Data(file)

            # Compute metrics
            metrics = data_extraction(data)
            all_metrics.append({"Subject": subject, "File": file.name, **metrics})

            if _show_graphs:
                # Extract relevant metrics
                head_pos = data.get(DataTag.HEAD_POSITION, data_type=DataType.VALUE, axis=DataAxis.ALL)
                head_vel = data.get(DataTag.HEAD_POSITION, data_type=DataType.VELOCITY, axis=DataAxis.ALL)
                head_acc = data.get(DataTag.HEAD_POSITION, data_type=DataType.ACCELERATION, axis=DataAxis.ALL)

                # Plot head position
                plt.figure(file.name)
                plt.title(f"{subject} - {file.stem}")
                plt.subplot(3, 1, 1)
                plt.plot(data.time, head_pos)
                for idx in DataIndicesExtraction:
                    if metrics[DataMetrics.JUMP_INDICES][idx] is not None:
                        plt.axvline(data.time[metrics[DataMetrics.JUMP_INDICES][idx]], color="r", linestyle="--")
                plt.ylabel("Head position (m)")
                plt.subplot(3, 1, 2)
                plt.plot(data.time, head_vel)
                for idx in DataIndicesExtraction:
                    if metrics[DataMetrics.JUMP_INDICES][idx] is not None:
                        plt.axvline(data.time[metrics[DataMetrics.JUMP_INDICES][idx]], color="r", linestyle="--")
                plt.ylabel("Head Velocity (m)")
                plt.subplot(3, 1, 3)
                plt.plot(data.time, head_acc)
                for idx in DataIndicesExtraction:
                    if metrics[DataMetrics.JUMP_INDICES][idx] is not None:
                        plt.axvline(data.time[metrics[DataMetrics.JUMP_INDICES][idx]], color="r", linestyle="--")
                plt.ylabel("Head Acceleration (m/s²)")
                plt.xlabel("Time (s)")
                plt.tight_layout()
                plt.show()

    # Write the excel file from all_metrics
    header = ["Subject", "File"]
    for metric in DataMetrics:
        if metric == DataMetrics.JUMP_INDICES:
            continue
        header.append(metric.value)

    # Write the data to an Excel file
    data = []
    for metrics in all_metrics:
        row = [metrics["Subject"], metrics["File"]]
        for metric in DataMetrics:
            if metric == DataMetrics.JUMP_INDICES:
                continue
            row.append(metrics[metric])
        data.append(row)
    df = pd.DataFrame(data, columns=header)
    df.to_excel("metrics.xlsx", index=False)


if __name__ == "__main__":
    main()
