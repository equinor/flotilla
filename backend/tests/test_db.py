from flotilla.database.models import RobotDBModel


def test_mock_db(session):
    assert len(session.query(RobotDBModel.name).all()) == 2
