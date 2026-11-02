import styled from 'styled-components'

export const LiveCameraRow = styled.div<{ $compact: boolean }>`
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: ${({ $compact }) => ($compact ? '1rem' : '3rem')};
    padding: 1rem 0;
    min-width: 0;
`

export const LiveCamera = styled.div<{ $compact: boolean; $aspectRatio: number }>`
    min-width: 0;
    ${({ $compact, $aspectRatio }) =>
        $compact
            ? `
                flex: ${$aspectRatio} 1 ${$aspectRatio * 12}rem;
                min-width: min(100%, ${$aspectRatio * 12}rem);
                max-width: min(100%, ${$aspectRatio * 20}rem);
            `
            : 'width: min(100%, 32rem);'}
`
