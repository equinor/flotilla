import { Icon, Typography } from '@equinor/eds-core-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Task, TaskStatus } from 'models/Task'
import { useInstallationContext } from 'components/Contexts/InstallationContext'
import { Icons } from 'utils/icons'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import { formatDateTime } from 'utils/StringFormatting'
import { useInspectionsContext } from 'components/Contexts/InpectionsContext'
import {
    StyledBottomContent,
    StyledCloseButton,
    StyledDialog,
    StyledDialogContent,
    StyledDialogHeader,
    StyledDialogInspectionView,
    StyledImageCard,
    StyledImagesSection,
    StyledInfoContent,
    StyledInspection,
    StyledInspectionCards,
    StyledInspectionContent,
    StyledInspectionData,
    StyledInspectionImage,
    StyledSection,
} from './InspectionStyles'

interface InspectionDialogViewProps {
    task: Task
    tasks: Task[]
}

const getMeta = async (url: string) => {
    const image = new Image()
    image.src = url
    await image.decode()
    return image
}

export const InspectionDialogView = ({ task, tasks }: InspectionDialogViewProps) => {
    const { TranslateText } = useLanguageContext()
    const { installationName } = useInstallationContext()
    const [inspectionImage, setInspectionImage] = useState<HTMLImageElement>(document.createElement('img'))
    const imageObjectURL = useRef<string>('')

    const { switchSelectedInspectionTask, mappingInspectionTasksObjectURL } = useInspectionsContext()

    const updateImage = useCallback(() => {
        if (task.isarTaskId && mappingInspectionTasksObjectURL[task.isarTaskId]) {
            imageObjectURL.current = mappingInspectionTasksObjectURL[task.isarTaskId]

            getMeta(imageObjectURL.current).then((img) => {
                const inspectionCanvas = document.getElementById('inspectionCanvas') as HTMLCanvasElement
                if (inspectionCanvas) {
                    inspectionCanvas.width = img.width
                    inspectionCanvas.height = img.height
                    let context = inspectionCanvas.getContext('2d')
                    if (context) {
                        context.drawImage(img, 0, 0)
                    }
                }
                setInspectionImage(img)
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [mappingInspectionTasksObjectURL])

    useEffect(() => {
        updateImage()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [mappingInspectionTasksObjectURL, inspectionImage])

    return (
        <>
            {imageObjectURL.current !== '' && (
                <StyledDialog open={true}>
                    <StyledDialogContent>
                        <StyledDialogHeader>
                            <Typography variant="accordion_header" group="ui">
                                {TranslateText('Inspection report')}
                            </Typography>
                            <StyledCloseButton variant="ghost" onClick={() => switchSelectedInspectionTask(undefined)}>
                                <Icon name={Icons.Clear} size={24} />
                            </StyledCloseButton>
                        </StyledDialogHeader>
                        <StyledDialogInspectionView>
                            <div>
                                <StyledInspection id="inspectionCanvas" />
                                <StyledBottomContent>
                                    <StyledInfoContent>
                                        <Typography variant="caption">{TranslateText('Installation') + ':'}</Typography>
                                        <Typography variant="body_short">{installationName}</Typography>
                                    </StyledInfoContent>
                                    <StyledInfoContent>
                                        <Typography variant="caption">{TranslateText('Tag') + ':'}</Typography>
                                        <Typography variant="body_short">{task.tagId}</Typography>
                                    </StyledInfoContent>
                                    {task.description && (
                                        <StyledInfoContent>
                                            <Typography variant="caption">
                                                {TranslateText('Description') + ':'}
                                            </Typography>
                                            <Typography variant="body_short">{task.description}</Typography>
                                        </StyledInfoContent>
                                    )}
                                    {task.endTime && (
                                        <StyledInfoContent>
                                            <Typography variant="caption">
                                                {TranslateText('Timestamp') + ':'}
                                            </Typography>
                                            <Typography variant="body_short">
                                                {formatDateTime(task.endTime, 'dd.MM.yy - HH:mm')}
                                            </Typography>
                                        </StyledInfoContent>
                                    )}
                                </StyledBottomContent>
                            </div>
                            <InspectionsViewSection tasks={tasks} dialogView={true} />
                        </StyledDialogInspectionView>
                    </StyledDialogContent>
                </StyledDialog>
            )}
        </>
    )
}

interface InspectionsViewSectionProps {
    tasks: Task[]
    dialogView?: boolean | undefined
}

export const InspectionsViewSection = ({ tasks, dialogView }: InspectionsViewSectionProps) => {
    const { TranslateText } = useLanguageContext()
    const { switchSelectedInspectionTask } = useInspectionsContext()

    return (
        <>
            <StyledSection
                style={{
                    width: dialogView ? '350px' : 'auto',
                    borderColor: dialogView ? 'white' : 'auto',
                    padding: dialogView ? '0px' : 'auto',
                    maxHeight: dialogView ? '60vh' : 'auto',
                }}
            >
                {!dialogView && <Typography variant="h4">{TranslateText('Last completed inspection')}</Typography>}
                <StyledImagesSection>
                    <StyledInspectionCards>
                        {Object.keys(tasks).length > 0 &&
                            tasks.map(
                                (task) =>
                                    task.status === TaskStatus.Successful && (
                                        <StyledImageCard
                                            key={task.isarTaskId}
                                            onClick={() => switchSelectedInspectionTask(task)}
                                        >
                                            <GetInspectionImage task={task} tasks={tasks} />
                                            <StyledInspectionData>
                                                {task.tagId && (
                                                    <StyledInspectionContent>
                                                        <Typography variant="caption">
                                                            {TranslateText('Tag') + ':'}
                                                        </Typography>
                                                        <Typography variant="body_short">{task.tagId}</Typography>
                                                    </StyledInspectionContent>
                                                )}
                                                {task.endTime && (
                                                    <StyledInspectionContent>
                                                        <Typography variant="caption">
                                                            {TranslateText('Timestamp') + ':'}
                                                        </Typography>
                                                        <Typography variant="body_short">
                                                            {formatDateTime(task.endTime!, 'dd.MM.yy - HH:mm')}
                                                        </Typography>
                                                    </StyledInspectionContent>
                                                )}
                                            </StyledInspectionData>
                                        </StyledImageCard>
                                    )
                            )}
                    </StyledInspectionCards>
                </StyledImagesSection>
            </StyledSection>
        </>
    )
}

interface GetInspectionImageProps {
    task: Task
    tasks: Task[]
}

const GetInspectionImage = ({ task, tasks }: GetInspectionImageProps) => {
    const imageObjectURL = useRef<string>('')
    const [inspectionImage, setInspectionImage] = useState<HTMLImageElement>(document.createElement('img'))

    const { switchSelectedInspectionTasks, mappingInspectionTasksObjectURL } = useInspectionsContext()

    useEffect(() => {
        switchSelectedInspectionTasks(tasks)
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [tasks])

    const updateImage = useCallback(() => {
        if (task.isarTaskId && mappingInspectionTasksObjectURL[task.isarTaskId]) {
            imageObjectURL.current = mappingInspectionTasksObjectURL[task.isarTaskId]

            getMeta(imageObjectURL.current).then((img) => {
                const inspectionCanvas = document.getElementById(task.isarTaskId!) as HTMLCanvasElement
                if (inspectionCanvas) {
                    inspectionCanvas.width = img.width
                    inspectionCanvas.height = img.height
                    let context = inspectionCanvas.getContext('2d')
                    if (context) {
                        context.drawImage(img, 0, 0)
                    }
                }
                setInspectionImage(img)
            })
        }
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [mappingInspectionTasksObjectURL])

    useEffect(() => {
        updateImage()
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [mappingInspectionTasksObjectURL, inspectionImage])

    return <StyledInspectionImage id={task.isarTaskId} />
}
