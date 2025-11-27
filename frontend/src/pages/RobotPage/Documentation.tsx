import { Typography, Icon } from '@equinor/eds-core-react'
import { Icons } from 'utils/icons'
import { tokens } from '@equinor/eds-tokens'
import { useLanguageContext } from 'components/Contexts/LanguageContext'
import styled from 'styled-components'
import { DocumentInfo } from 'models/DocumentInfo'

const DocumentStyle = styled.div`
    display: flex;
    gap: 1rem;
    align-items: center;
`

export const DocumentationSection = ({ documentation }: { documentation: DocumentInfo[] }) => {
    const { TranslateText } = useLanguageContext()

    return (
        <>
            <Typography variant="h2">{TranslateText('Documentation')}</Typography>
            {documentation.map((documentInfo, index) => (
                <DocumentStyle key={index}>
                    <Icon
                        name={Icons.FileDescription}
                        color={tokens.colors.interactive.primary__resting.hex}
                        size={24}
                    />
                    <Typography variant="h4" color={tokens.colors.interactive.primary__resting.hex}>
                        <a href={documentInfo.url}>{documentInfo.name}</a>
                    </Typography>
                </DocumentStyle>
            ))}
        </>
    )
}
