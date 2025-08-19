from enum import Enum
import numpy as np
from pathlib import Path
import pandas as pd

from .maths import central_derivative, fit_confidence_ellipse


class DataAxis(Enum):
    X = [0]
    Y = [1]
    Z = [2]
    LATERAL = [0]
    VERTICAL = [1]
    FRONTAL = [2]
    HORIZONTAL = [0, 2]
    ALL = [0, 1, 2]


class DataTag(Enum):
    HEAD_POSITION = ["Head_Pos.X", "Head_Pos.Y", "Head_Pos.Z"]
    HEAD_ROTATION = ["Head_Rot.X", "Head_Rot.Y", "Head_Rot.Z"]
    LEFT_HAND_POSITION = ["LeftHand_Pos.X", "LeftHand_Pos.Y", "LeftHand_Pos.Z"]
    LEFT_HAND_ROTATION = ["LeftHand_Rot.X", "LeftHand_Rot.Y", "LeftHand_Rot.Z"]
    RIGHT_HAND_POSITION = ["RightHand_Pos.X", "RightHand_Pos.Y", "RightHand_Pos.Z"]
    RIGHT_HAND_ROTATION = ["RightHand_Rot.X", "RightHand_Rot.Y", "RightHand_Rot.Z"]


class DataType(Enum):
    VALUE = "value"
    VELOCITY = "velocity"
    ACCELERATION = "acceleration"


class Data:
    def __init__(self, data_path: Path):
        self._data_path = data_path
        self._df = pd.read_csv(data_path)

    @property
    def shape(self) -> tuple[int, int]:
        """Returns the shape of the DataFrame."""
        return self._df.shape

    @property
    def _header(self) -> pd.Index:
        return self._df.columns

    @property
    def time(self) -> pd.Series:
        """Returns the time in seconds since the start of the recording."""
        return self._df["Frame"] - self._df["Frame"].min()

    def get(
        self, tag: DataTag, t: slice = slice(None), data_type: DataType = DataType.VALUE, axis: DataAxis = DataAxis.ALL
    ) -> pd.DataFrame:
        """Returns the specified data type."""
        value = self._df[tag.value]

        if data_type == DataType.VALUE:
            return value.iloc[t, axis.value]

        velocity = central_derivative(value, self.time, window=10)
        if data_type == DataType.VELOCITY:
            return velocity.iloc[t, axis.value]

        acceleration = central_derivative(velocity, self.time, window=10)
        if data_type == DataType.ACCELERATION:
            return acceleration.iloc[t, axis.value]

        raise ValueError(f"Unsupported data type: {data_type}")

    def horizontal_dispersion(self, tag: DataTag, t: slice = slice(None)) -> np.float64:
        """Returns the horizontal dispersion (2D) of the specified tag."""
        position = self.get(tag=tag, t=t, data_type=DataType.VALUE, axis=DataAxis.HORIZONTAL)
        ellipse_params = fit_confidence_ellipse(position, confidence=0.95)

        # Return the area of the ellipse as a measure of dispersion (π * a * b)
        return (ellipse_params["a"] * ellipse_params["b"] * np.pi).values[0]

    def displacement(self, tag: DataTag, t: slice = slice(None), axis: DataAxis = DataAxis.ALL) -> np.float64:
        """Returns the displacement of the specified tag."""
        if len(axis.value) > 1:
            raise ValueError("Displacement can only be computed for a single axis.")
        position = self.get(tag=tag, t=t, data_type=DataType.VALUE, axis=axis)
        return position.max() - position.min()

    def acceleration_peak(self, tag: DataTag, t: slice = slice(None), axis: DataAxis = DataAxis.ALL) -> np.float64:
        """Returns the peak acceleration of the specified tag."""
        acceleration = self.get(tag=tag, t=t, data_type=DataType.ACCELERATION, axis=axis)
        acceleration = np.sqrt((acceleration**2).sum(axis=1))
        return acceleration.max()

    def traveled_distance(self, tag: DataTag, t: slice = slice(None)) -> np.float64:
        """Returns the total traveled distance of the specified tag."""
        position = self.get(tag=tag, t=t, data_type=DataType.VALUE, axis=DataAxis.ALL)
        diffs = position.diff().fillna(0)
        distances = np.sqrt((diffs**2).sum(axis=1))
        return (distances.cumsum()).values[-1]
