from flotilla.database.models import Robot


def test_mock_db(session):
    assert len(session.query(Robot.name).all()) == 2
