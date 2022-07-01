export class VideoStream {
    id?: string = 'defaultId'
    name?: string = 'defaultName'
    robotId?: string = 'defaltRobotId'
    url?: string = 'defaultUrl'

    constructor(id?: string, name?: string, robotId?: string, url?: string) {
        this.id = id
        this.name = name
        this.robotId = robotId
        this.url = url
    }
}
