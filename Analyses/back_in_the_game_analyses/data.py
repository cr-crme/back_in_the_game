from pathlib import Path
import pandas as pd


class Data:
    def __init__(self, data_path: Path):
        self._data_path = data_path
        self._df = pd.read_csv(data_path)

    @property
    def header(self) -> pd.Index:
        return self._df.columns

    @property
    def time(self) -> pd.Series:
        """Returns the time in seconds since the start of the recording."""
        return self._df["Frame"] - self._df["Frame"].min()

    @property
    def head_position(self) -> pd.DataFrame:
        """Returns the head position in m."""
        tag = ["Head_Pos.X", "Head_Pos.Y", "Head_Pos.Z"]
        return self._df[tag]

    @property
    def head_velocity(self) -> pd.DataFrame:
        """Returns the head velocity in m/s."""
        df = self.head_position
        return (df.shift(-1) - df.shift(1)) / 2

    @property
    def head_rotation(self) -> pd.DataFrame:
        """Returns the head rotation in degrees."""
        tag = ["Head_Rot.X", "Head_Rot.Y", "Head_Rot.Z"]
        return self._df[tag]

    @property
    def left_hand_position(self) -> pd.DataFrame:
        """Returns the left hand position in m."""
        tag = ["LeftHand_Pos.X", "LeftHand_Pos.Y", "LeftHand_Pos.Z"]
        return self._df[tag]

    @property
    def left_hand_rotation(self) -> pd.DataFrame:
        """Returns the left hand rotation in degrees."""
        tag = ["LeftHand_Rot.X", "LeftHand_Rot.Y", "LeftHand_Rot.Z"]
        return self._df[tag]

    @property
    def left_hand_velocity(self) -> pd.DataFrame:
        """Returns the left hand velocity in m/s."""
        tag = ["LeftHand_Vel.X", "LeftHand_Vel.Y", "LeftHand_Vel.Z"]
        return self._df[tag]

    @property
    def right_hand_position(self) -> pd.DataFrame:
        """Returns the right hand position in m."""
        tag = ["RightHand_Pos.X", "RightHand_Pos.Y", "RightHand_Pos.Z"]
        return self._df[tag]

    @property
    def right_hand_rotation(self) -> pd.DataFrame:
        """Returns the right hand rotation in degrees."""
        tag = ["RightHand_Rot.X", "RightHand_Rot.Y", "RightHand_Rot.Z"]
        return self._df[tag]
