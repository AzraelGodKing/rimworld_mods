#!/usr/bin/env perl
# Extract DefInjected LanguageData skeletons from RimWorld Defs XMLs.
# Values remain English for later translation.
# Usage: perl extract_definjected.pl <modRoot> <langFolderName>
use strict;
use warnings;
use File::Find;
use File::Basename;
use File::Path qw(make_path);

my $mod  = shift or die "Usage: $0 <modRoot> <lang>\n";
my $lang = shift or die "Usage: $0 <modRoot> <lang>\n";
my $defs = "$mod/Defs";
die "No Defs at $defs\n" unless -d $defs;

my %by_file;    # "DefType/basename.xml" => [ [tag, value], ... ]

sub escape_xml {
    my ($s) = @_;
    $s =~ s/&/&amp;/g;
    $s =~ s/</&lt;/g;
    $s =~ s/>/&gt;/g;
    return $s;
}

find(
    {
        wanted => sub {
            return unless -f $_ && /\.xml$/i;
            my $path = $File::Find::name;
            open my $fh, '<:encoding(UTF-8)', $path or return;
            local $/;
            my $xml = <$fh>;
            close $fh;
            $xml =~ s/<!--.*?-->//sg;

            # Match ThingDef, ResearchProjectDef, DeepColony.PerkDef, etc.
            while ( $xml =~
                m{<(?:[\w]+\.)?(\w+Def)\b([^>]*)>(.*?)</(?:[\w]+\.)?\1>}sg )
            {
                my ( $dtype, $attrs, $body ) = ( $1, $2, $3 );
                next if $attrs =~ /Abstract=["']?True/i;
                # Skip abstract named bases without defName
                my ($defName) = $body =~ m{<defName>\s*([^<]+?)\s*</defName>}s;
                next unless defined $defName;
                $defName =~ s/^\s+|\s+$//g;

                my @entries;

                for my $field (
                    qw(label description labelShort gerund verb jobString
                    letterLabel letterText baseInspectLine customLabel
                    productsLabel reportString triggerMessage
                    labelFemale labelFemalePlural labelMale
                    pawnLabel labelSolid)
                  )
                {
                    while ( $body =~ m{<$field>(.*?)</$field>}sg ) {
                        my $val = $1;
                        $val =~ s/^\s+|\s+$//g;
                        next if $val eq '';
                        push @entries, [ "$defName.$field", $val ];
                    }
                }

                if ( $body =~ m{<stages>(.*?)</stages>}s ) {
                    my $stages = $1;
                    my $i      = 0;
                    while ( $stages =~ m{<li\b[^>]*>(.*?)</li>}sg ) {
                        my $st = $1;
                        for my $field (qw(label description labelSocial)) {
                            if ( $st =~ m{<$field>(.*?)</$field>}s ) {
                                my $val = $1;
                                $val =~ s/^\s+|\s+$//g;
                                push @entries, [ "$defName.stages_$i.$field", $val ]
                                  if $val ne '';
                            }
                        }
                        $i++;
                    }
                }

                next unless @entries;
                my $base = basename($path);
                my $key  = "$dtype/$base";
                push @{ $by_file{$key} }, @entries;
            }
        },
        no_chdir => 1,
    },
    $defs
);

my $total = 0;
for my $key ( sort keys %by_file ) {
    my ( $dtype, $base ) = split m{/}, $key, 2;
    my $out_dir = "$mod/Languages/$lang/DefInjected/$dtype";
    make_path($out_dir);
    my $out = "$out_dir/$base";
    open my $oh, '>:encoding(UTF-8)', $out or die "Cannot write $out: $!";
    print {$oh} qq{<?xml version="1.0" encoding="utf-8"?>\n<LanguageData>\n\n};
    my %seen;
    for my $e ( @{ $by_file{$key} } ) {
        my ( $tag, $val ) = @$e;
        next if $seen{$tag}++;
        print {$oh} "  <$tag>", escape_xml($val), "</$tag>\n";
        $total++;
    }
    print {$oh} "\n</LanguageData>\n";
    close $oh;
    print "Wrote $out (", scalar(keys %seen), " tags)\n";
}
print "Done: ", scalar(keys %by_file), " files, $total tags\n";
